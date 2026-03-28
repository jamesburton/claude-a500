using AmigaA500.Core;
using AmigaA500.Core.Cpu;
using AmigaA500.Core.Floppy;

namespace AmigaA500.Tests.Integration;

public class BootTests
{
    [Fact]
    public void Reset_LoadsStackAndPCFromRom()
    {
        // Create a minimal ROM that sets SSP=$00FF00 and PC=$FC0008
        var rom = new byte[256 * 1024];
        // Vector 0 (reset SSP) at offset 0
        rom[0] = 0x00; rom[1] = 0x00; rom[2] = 0xFF; rom[3] = 0x00;
        // Vector 1 (reset PC) at offset 4
        rom[4] = 0x00; rom[5] = 0xFC; rom[6] = 0x00; rom[7] = 0x08;
        // NOP at offset 8
        rom[8] = 0x4E; rom[9] = 0x71;

        var amiga = new Amiga(rom);
        amiga.Reset();

        Assert.Equal(0x0000FF00u, amiga.Cpu.A[7]);
        Assert.Equal(0x00FC0008u, amiga.Cpu.PC);
        Assert.True(amiga.Cpu.Supervisor);
    }

    [Fact]
    public void Reset_OverlayActive()
    {
        var rom = new byte[256 * 1024];
        rom[0] = 0x00; rom[1] = 0x00; rom[2] = 0xFF; rom[3] = 0x00;
        rom[4] = 0x00; rom[5] = 0xFC; rom[6] = 0x00; rom[7] = 0x08;
        rom[8] = 0x4E; rom[9] = 0x71;

        var amiga = new Amiga(rom);
        amiga.Reset();

        Assert.True(amiga.Bus.Overlay);
    }

    [Fact]
    public void Step_ExecutesInstructions()
    {
        var rom = new byte[256 * 1024];
        // SSP = $FF00
        rom[0] = 0x00; rom[1] = 0x00; rom[2] = 0xFF; rom[3] = 0x00;
        // PC = $FC0008
        rom[4] = 0x00; rom[5] = 0xFC; rom[6] = 0x00; rom[7] = 0x08;
        // MOVEQ #1, D0 at offset 8
        rom[8] = 0x70; rom[9] = 0x01;
        // MOVEQ #2, D1 at offset 10
        rom[10] = 0x72; rom[11] = 0x02;

        var amiga = new Amiga(rom);
        amiga.Reset();

        amiga.Step(); // Execute MOVEQ #1, D0
        Assert.Equal(1u, amiga.Cpu.D[0]);

        amiga.Step(); // Execute MOVEQ #2, D1
        Assert.Equal(2u, amiga.Cpu.D[1]);
    }

    [Fact]
    public void AdfDisk_CorrectSize()
    {
        var data = new byte[AdfDisk.TotalSize];
        var disk = new AdfDisk(data);
        Assert.Equal(901120, AdfDisk.TotalSize);
    }

    [Fact]
    public void AdfDisk_RejectsWrongSize()
    {
        var data = new byte[1000];
        Assert.Throws<ArgumentException>(() => new AdfDisk(data));
    }

    [Fact]
    public void AdfDisk_ReadBootblock()
    {
        var data = new byte[AdfDisk.TotalSize];
        // Write "DOS\0" magic
        data[0] = (byte)'D'; data[1] = (byte)'O'; data[2] = (byte)'S'; data[3] = 0;
        var disk = new AdfDisk(data);

        var bootblock = disk.ReadBootblock();
        Assert.Equal(1024, bootblock.Length);
        Assert.Equal((byte)'D', bootblock[0]);
        Assert.Equal((byte)'O', bootblock[1]);
        Assert.Equal((byte)'S', bootblock[2]);
    }

    [Fact]
    public void AdfDisk_DiskType()
    {
        var data = new byte[AdfDisk.TotalSize];
        data[0] = (byte)'D'; data[1] = (byte)'O'; data[2] = (byte)'S'; data[3] = 0;
        var disk = new AdfDisk(data);
        Assert.Equal(DiskType.OFS, disk.GetDiskType());

        data[3] = 1;
        disk = new AdfDisk(data);
        Assert.Equal(DiskType.FFS, disk.GetDiskType());
    }

    [Fact]
    public void FloppyDrive_StepAndTrackZero()
    {
        var drive = new FloppyDrive();
        var data = new byte[AdfDisk.TotalSize];
        drive.InsertDisk(new AdfDisk(data));

        Assert.True(drive.AtTrackZero);

        drive.StepHead(towardCenter: true);
        Assert.Equal(1, drive.CurrentCylinder);
        Assert.False(drive.AtTrackZero);

        drive.StepHead(towardCenter: false);
        Assert.Equal(0, drive.CurrentCylinder);
        Assert.True(drive.AtTrackZero);
    }

    [Fact]
    public void FloppyDrive_DoesNotStepBelowZero()
    {
        var drive = new FloppyDrive();
        var data = new byte[AdfDisk.TotalSize];
        drive.InsertDisk(new AdfDisk(data));

        drive.StepHead(towardCenter: false); // Already at 0
        Assert.Equal(0, drive.CurrentCylinder);
    }
}
