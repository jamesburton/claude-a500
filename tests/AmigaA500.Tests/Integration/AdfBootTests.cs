using AmigaA500.Core;
using AmigaA500.Core.Floppy;

namespace AmigaA500.Tests.Integration;

/// <summary>
/// Tests for ADF boot sequence — creates synthetic ADF images and verifies
/// the emulator can load and begin executing bootblock code.
/// </summary>
public class AdfBootTests
{
    private static byte[] CreateMinimalBootableAdf()
    {
        var data = new byte[AdfDisk.TotalSize];

        // Bootblock: DOS\0 header
        data[0] = (byte)'D';
        data[1] = (byte)'O';
        data[2] = (byte)'S';
        data[3] = 0; // OFS

        // Boot code at offset 12 (after header):
        // MOVEQ #42, D0  ($702A)
        // RTS             ($4E75)
        data[12] = 0x70; data[13] = 0x2A;
        data[14] = 0x4E; data[15] = 0x75;

        // Fix checksum (bootblock checksum at offset 4)
        FixBootblockChecksum(data);
        return data;
    }

    private static void FixBootblockChecksum(byte[] data)
    {
        // Clear checksum field first
        data[4] = data[5] = data[6] = data[7] = 0;

        // Calculate sum of all longs
        uint sum = 0;
        for (int i = 0; i < 1024; i += 4)
        {
            uint word = (uint)(data[i] << 24 | data[i + 1] << 16 | data[i + 2] << 8 | data[i + 3]);
            uint prev = sum;
            sum += word;
            if (sum < prev) sum++; // Carry
        }

        // Checksum = ~sum (so total becomes $FFFFFFFF)
        uint checksum = ~sum;
        data[4] = (byte)(checksum >> 24);
        data[5] = (byte)(checksum >> 16);
        data[6] = (byte)(checksum >> 8);
        data[7] = (byte)checksum;
    }

    [Fact]
    public void AdfDisk_BootableChecksum_Valid()
    {
        var data = CreateMinimalBootableAdf();
        var disk = new AdfDisk(data);
        Assert.True(disk.IsBootable());
    }

    [Fact]
    public void AdfDisk_BootableChecksum_Invalid()
    {
        var data = new byte[AdfDisk.TotalSize];
        data[0] = (byte)'D'; data[1] = (byte)'O'; data[2] = (byte)'S'; data[3] = 0;
        // Don't fix checksum
        var disk = new AdfDisk(data);
        Assert.False(disk.IsBootable());
    }

    [Fact]
    public void SyntheticAdf_ContainsBootCode()
    {
        var data = CreateMinimalBootableAdf();
        var disk = new AdfDisk(data);
        var bootblock = disk.ReadBootblock();

        // Verify our boot code is present
        Assert.Equal(0x70, bootblock[12]); // MOVEQ #42, D0
        Assert.Equal(0x2A, bootblock[13]);
        Assert.Equal(0x4E, bootblock[14]); // RTS
        Assert.Equal(0x75, bootblock[15]);
    }

    [Fact]
    public void SyntheticAdf_BootCodeExecutes()
    {
        // Create a minimal ROM that jumps to bootblock code
        var rom = new byte[256 * 1024];
        // Reset SSP
        rom[0] = 0x00; rom[1] = 0x00; rom[2] = 0xFF; rom[3] = 0x00;
        // Reset PC → $FC0008
        rom[4] = 0x00; rom[5] = 0xFC; rom[6] = 0x00; rom[7] = 0x08;

        // Code at $FC0008: Load boot code address into A0 and JSR to it
        // We'll directly load the bootblock code into chip RAM at $1000 and jump to it
        int idx = 8;
        // MOVE.L #$0000100C, A0 — LEA $100C, A0
        rom[idx++] = 0x41; rom[idx++] = 0xF9;
        rom[idx++] = 0x00; rom[idx++] = 0x00;
        rom[idx++] = 0x10; rom[idx++] = 0x0C;
        // Clear OVL: write to CIA-A PRA to disable overlay
        // MOVE.B #$00, $BFE001 — but simplified, just clear overlay via a trick
        // Actually, let's just use JSR (A0)
        rom[idx++] = 0x4E; rom[idx++] = 0x90; // JSR (A0)
        // After RTS, STOP here
        rom[idx++] = 0x4E; rom[idx++] = 0x72; // STOP
        rom[idx++] = 0x27; rom[idx++] = 0x00; // #$2700

        var amiga = new Amiga(rom);
        amiga.Reset();

        // Manually load bootblock data into chip RAM at $1000
        var adfData = CreateMinimalBootableAdf();
        amiga.Bus.Overlay = false; // Allow writing to chip RAM

        // Copy bootblock to chip RAM
        for (int i = 0; i < 1024; i++)
            amiga.Bus.WriteByte((uint)(0x1000 + i), adfData[i]);

        // Re-enable overlay for ROM execution
        amiga.Bus.Overlay = true;
        amiga.Cpu.Reset();

        // Execute instructions
        for (int i = 0; i < 20; i++)
        {
            amiga.Step();
            if (amiga.Cpu.Halted) break;
        }

        // After boot code: D0 should be 42 (from MOVEQ #42, D0)
        Assert.Equal(42u, amiga.Cpu.D[0]);
    }

    [Fact]
    public void AdfDisk_TrackSectorAccess()
    {
        var data = new byte[AdfDisk.TotalSize];
        // Write marker at track 1, side 0, sector 5
        int offset = (1 * 2 + 0) * AdfDisk.BytesPerTrack + 5 * AdfDisk.BytesPerSector;
        data[offset] = 0xCA;
        data[offset + 1] = 0xFE;

        var disk = new AdfDisk(data);
        var sector = disk.ReadSector(1, 0, 5);
        Assert.Equal(0xCA, sector[0]);
        Assert.Equal(0xFE, sector[1]);
    }
}
