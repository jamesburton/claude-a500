using AmigaA500.Core;
using AmigaA500.Core.Floppy;

namespace AmigaA500.Tests.Integration;

public class SystemIntegrationTests
{
    private static byte[] CreateSimpleRom(params ushort[] program)
    {
        var rom = new byte[256 * 1024];
        // Reset SSP
        rom[0] = 0x00; rom[1] = 0x00; rom[2] = 0xFF; rom[3] = 0x00;
        // Reset PC → $FC0008
        rom[4] = 0x00; rom[5] = 0xFC; rom[6] = 0x00; rom[7] = 0x08;

        for (int i = 0; i < program.Length; i++)
        {
            rom[8 + i * 2] = (byte)(program[i] >> 8);
            rom[9 + i * 2] = (byte)(program[i] & 0xFF);
        }
        return rom;
    }

    [Fact]
    public void RunFrame_CompletesWithoutCrash()
    {
        // ROM with NOP loop
        var rom = CreateSimpleRom(0x4E71, 0x4E71, 0x4E71); // NOP NOP NOP
        var amiga = new Amiga(rom);
        amiga.Reset();

        // Should not throw
        amiga.RunFrame();
        Assert.True(amiga.Cpu.TotalCycles > 0);
    }

    [Fact]
    public void InsertDisk_SetsReady()
    {
        var rom = CreateSimpleRom(0x4E71);
        var amiga = new Amiga(rom);
        amiga.Reset();

        var diskData = new byte[AdfDisk.TotalSize];
        var disk = new AdfDisk(diskData);
        amiga.InsertDisk(0, disk);

        Assert.True(amiga.Drives[0].DiskInserted);
    }

    [Fact]
    public void CiaA_OvlBit_ControlsOverlay()
    {
        var rom = CreateSimpleRom(
            0x4E71 // NOP
        );
        var amiga = new Amiga(rom);
        amiga.Reset();

        Assert.True(amiga.Bus.Overlay);

        // Clear OVL by writing to CIA-A PRA
        amiga.CiaA.DDRA = 0x03; // Output for bits 0-1
        amiga.CiaA.PRA = 0x00;  // Clear OVL bit

        amiga.Step(); // Process state
        Assert.False(amiga.Bus.Overlay);
    }

    [Fact]
    public void VblankInterrupt_Fires()
    {
        var rom = CreateSimpleRom(0x4E71);
        var amiga = new Amiga(rom);
        amiga.Reset();

        // Enable VERTB interrupt
        amiga.Custom.WriteRegister(0x09A, 0xC020); // INTENA: SET + INTEN + VERTB

        amiga.RunFrame();

        // VERTB should have been requested
        Assert.NotEqual(0, amiga.Custom.INTREQ & 0x0020);
    }

    [Fact]
    public void Framebuffer_Initialized()
    {
        var rom = CreateSimpleRom(0x4E71);
        var amiga = new Amiga(rom);
        Assert.Equal(320 * 256, amiga.Framebuffer.Length);
    }

    [Fact]
    public void SaveState_SerializesSuccessfully()
    {
        var rom = CreateSimpleRom(0x702A); // MOVEQ #42, D0
        var amiga = new Amiga(rom);
        amiga.Reset();
        amiga.Step();

        string json = SaveState.Serialize(amiga);
        Assert.Contains("42", json); // D0 should be 42
        Assert.Contains("PC", json);
    }
}
