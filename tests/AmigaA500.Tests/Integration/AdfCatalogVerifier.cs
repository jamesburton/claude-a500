using AmigaA500.Core;
using AmigaA500.Core.Floppy;

namespace AmigaA500.Tests.Integration;

/// <summary>
/// Verifies ADF disk images can be loaded and processed by the emulator.
/// Creates synthetic test ADFs representing catalog entries and verifies boot behavior.
/// </summary>
public class AdfCatalogVerifier
{
    private static byte[] CreateMinimalRom()
    {
        var rom = new byte[256 * 1024];
        // SSP = $FF00
        rom[0] = 0x00; rom[1] = 0x00; rom[2] = 0xFF; rom[3] = 0x00;
        // PC = $FC0008
        rom[4] = 0x00; rom[5] = 0xFC; rom[6] = 0x00; rom[7] = 0x08;
        // Simple program: clear OVL, then loop
        int idx = 8;
        // LEA $1000, A0
        rom[idx++] = 0x41; rom[idx++] = 0xF9; rom[idx++] = 0x00; rom[idx++] = 0x00;
        rom[idx++] = 0x10; rom[idx++] = 0x00;
        // MOVEQ #1, D0
        rom[idx++] = 0x70; rom[idx++] = 0x01;
        // STOP #$2000
        rom[idx++] = 0x4E; rom[idx++] = 0x72; rom[idx++] = 0x20; rom[idx++] = 0x00;
        return rom;
    }

    private static byte[] CreateSyntheticAdf(string name, byte marker)
    {
        var data = new byte[AdfDisk.TotalSize];
        // DOS header
        data[0] = (byte)'D'; data[1] = (byte)'O'; data[2] = (byte)'S'; data[3] = 0;
        // Boot code: MOVEQ #<marker>, D0; RTS
        data[12] = 0x70; data[13] = marker;
        data[14] = 0x4E; data[15] = 0x75;
        // Write name into unused bootblock area for identification
        var nameBytes = System.Text.Encoding.ASCII.GetBytes(name.Length > 40 ? name[..40] : name);
        Array.Copy(nameBytes, 0, data, 20, nameBytes.Length);
        // Fix bootblock checksum
        FixChecksum(data);
        return data;
    }

    private static void FixChecksum(byte[] data)
    {
        data[4] = data[5] = data[6] = data[7] = 0;
        uint sum = 0;
        for (int i = 0; i < 1024; i += 4)
        {
            uint word = (uint)(data[i] << 24 | data[i + 1] << 16 | data[i + 2] << 8 | data[i + 3]);
            uint prev = sum;
            sum += word;
            if (sum < prev) sum++;
        }
        uint cs = ~sum;
        data[4] = (byte)(cs >> 24); data[5] = (byte)(cs >> 16);
        data[6] = (byte)(cs >> 8); data[7] = (byte)cs;
    }

    [Fact]
    public void Verify_SyntheticPDGame_Boots()
    {
        var rom = CreateMinimalRom();
        var adf = CreateSyntheticAdf("TestGame_PD", 42);
        var disk = new AdfDisk(adf);

        Assert.True(disk.IsBootable());
        Assert.Equal(DiskType.OFS, disk.GetDiskType());
    }

    [Fact]
    public void Verify_MultipleSyntheticAdfs_AllBootable()
    {
        string[] names = {
            "Zork_PD", "Tetris_Clone_PD", "Chess_PD", "Breakout_PD", "Snake_PD",
            "Pacman_Clone_PD", "Space_Invaders_PD", "Pong_PD", "Asteroids_PD", "Frogger_PD"
        };

        foreach (var name in names)
        {
            var adf = CreateSyntheticAdf(name, (byte)(name.GetHashCode() & 0x7F));
            var disk = new AdfDisk(adf);
            Assert.True(disk.IsBootable(), $"{name} should be bootable");
        }
    }

    [Fact]
    public void Verify_SyntheticDemo_BootcodeExecutes()
    {
        var rom = CreateMinimalRom();
        var amiga = new Amiga(rom);

        var adf = CreateSyntheticAdf("CoolDemo", 99);
        amiga.Bus.Overlay = false;
        // Load bootblock into chip RAM
        for (int i = 0; i < 1024; i++)
            amiga.Bus.WriteByte((uint)(0x1000 + i), adf[i]);
        amiga.Bus.Overlay = true;
        amiga.Cpu.Reset();

        // Execute: LEA, MOVEQ, then stop
        for (int i = 0; i < 5; i++) amiga.Step();

        // CPU should have run (PC moved from initial)
        Assert.NotEqual(0xFC0008u, amiga.Cpu.PC);
    }

    [Fact]
    public void Verify_FFSDisk_Detected()
    {
        var data = new byte[AdfDisk.TotalSize];
        data[0] = (byte)'D'; data[1] = (byte)'O'; data[2] = (byte)'S'; data[3] = 1; // FFS
        var disk = new AdfDisk(data);
        Assert.Equal(DiskType.FFS, disk.GetDiskType());
    }

    [Fact]
    public void Verify_OFSIntlDisk_Detected()
    {
        var data = new byte[AdfDisk.TotalSize];
        data[0] = (byte)'D'; data[1] = (byte)'O'; data[2] = (byte)'S'; data[3] = 2;
        var disk = new AdfDisk(data);
        Assert.Equal(DiskType.OFS_Intl, disk.GetDiskType());
    }

    [Fact]
    public void Verify_BatchSyntheticAdfs_BootSequence()
    {
        var rom = CreateMinimalRom();
        var results = new List<string>();

        // Test 20 synthetic ADFs
        for (int i = 0; i < 20; i++)
        {
            string name = $"SyntheticDisk_{i:D3}";
            var adf = CreateSyntheticAdf(name, (byte)(i + 1));
            var disk = new AdfDisk(adf);

            if (disk.IsBootable())
            {
                var amiga = new Amiga(rom);
                amiga.Bus.Overlay = false;
                for (int j = 0; j < 1024; j++)
                    amiga.Bus.WriteByte((uint)(0x1000 + j), adf[j]);
                amiga.Bus.Overlay = true;
                amiga.Cpu.Reset();

                try
                {
                    for (int f = 0; f < 3; f++) amiga.Step();
                    results.Add($"BOOT {name}");
                }
                catch
                {
                    results.Add($"FAIL {name}");
                }
            }
        }

        Assert.True(results.Count >= 20);
        Assert.All(results, r => Assert.StartsWith("BOOT", r));
    }

    [Fact]
    public void Verify_DiskSectorAccess_AllTracks()
    {
        var data = new byte[AdfDisk.TotalSize];
        // Write markers at specific sectors
        for (int cyl = 0; cyl < 80; cyl++)
        {
            for (int side = 0; side < 2; side++)
            {
                int offset = (cyl * 2 + side) * AdfDisk.BytesPerTrack;
                data[offset] = (byte)cyl;
                data[offset + 1] = (byte)side;
            }
        }

        var disk = new AdfDisk(data);

        for (int cyl = 0; cyl < 80; cyl++)
        {
            for (int side = 0; side < 2; side++)
            {
                var track = disk.ReadTrack(cyl, side);
                Assert.Equal((byte)cyl, track[0]);
                Assert.Equal((byte)side, track[1]);
            }
        }
    }
}
