using AmigaA500.Core.Cartridge;

namespace AmigaA500.Tests.Cartridge;

public class ActionReplayTests
{
    [Fact]
    public void LoadRom_256KB_Succeeds()
    {
        var ar = new ActionReplay();
        var rom = new byte[262144];
        rom[0] = 0x4E; rom[1] = 0x71; // NOP at start
        Assert.True(ar.LoadRom(rom));
        Assert.True(ar.Enabled);
    }

    [Fact]
    public void LoadRom_64KB_Succeeds()
    {
        var ar = new ActionReplay();
        Assert.True(ar.LoadRom(new byte[65536]));
    }

    [Fact]
    public void LoadRom_BadSize_Fails()
    {
        var ar = new ActionReplay();
        Assert.False(ar.LoadRom(new byte[1000]));
        Assert.False(ar.Enabled);
    }

    [Fact]
    public void ReadByte_ReturnsRomData()
    {
        var ar = new ActionReplay();
        var rom = new byte[262144];
        rom[0] = 0xCA; rom[1] = 0xFE;
        ar.LoadRom(rom);

        Assert.Equal(0xCA, ar.ReadByte(ActionReplay.RomBase));
        Assert.Equal(0xFE, ar.ReadByte(ActionReplay.RomBase + 1));
    }

    [Fact]
    public void ReadWord_ReturnsCorrectEndian()
    {
        var ar = new ActionReplay();
        var rom = new byte[262144];
        rom[0] = 0xDE; rom[1] = 0xAD;
        ar.LoadRom(rom);

        Assert.Equal(0xDEAD, ar.ReadWord(ActionReplay.RomBase));
    }

    [Fact]
    public void Freeze_SetsFlag()
    {
        var ar = new ActionReplay();
        ar.LoadRom(new byte[262144]);
        ar.Freeze();

        Assert.True(ar.FreezeRequested);
        Assert.True(ar.MenuActive);
    }

    [Fact]
    public void CheckAndClearFreeze_ClearsFlag()
    {
        var ar = new ActionReplay();
        ar.LoadRom(new byte[262144]);
        ar.Freeze();

        Assert.True(ar.CheckAndClearFreeze());
        Assert.False(ar.CheckAndClearFreeze()); // Already cleared
    }

    [Fact]
    public void Resume_ClearsMenu()
    {
        var ar = new ActionReplay();
        ar.LoadRom(new byte[262144]);
        ar.Freeze();
        ar.Resume();

        Assert.False(ar.MenuActive);
    }

    [Fact]
    public void HandlesAddress_InRange()
    {
        var ar = new ActionReplay();
        ar.LoadRom(new byte[262144]);

        Assert.True(ar.HandlesAddress(0xF00000));
        Assert.True(ar.HandlesAddress(0xF7FFFF));
        Assert.False(ar.HandlesAddress(0xF80000));
        Assert.False(ar.HandlesAddress(0xEFFFFF));
    }

    [Fact]
    public void WriteStatus_DisablesCartridge()
    {
        var ar = new ActionReplay();
        ar.LoadRom(new byte[262144]);
        Assert.True(ar.Enabled);

        ar.WriteStatus(0x00); // Disable
        Assert.False(ar.Enabled);
        Assert.False(ar.HandlesAddress(0xF00000));
    }

    [Fact]
    public void Reset_RestoresEnabled()
    {
        var ar = new ActionReplay();
        ar.LoadRom(new byte[262144]);
        ar.WriteStatus(0x00);
        ar.Reset();

        Assert.True(ar.Enabled);
        Assert.False(ar.MenuActive);
        Assert.False(ar.FreezeRequested);
    }

    [Fact]
    public void DetectsVersion_MkIII_From512KB()
    {
        var ar = new ActionReplay();
        ar.LoadRom(new byte[524288]);
        Assert.Equal(ActionReplayVersion.MkIII, ar.Version);
    }

    [Fact]
    public void DetectsVersion_MkI_From64KB()
    {
        var ar = new ActionReplay();
        ar.LoadRom(new byte[65536]);
        Assert.Equal(ActionReplayVersion.MkI, ar.Version);
    }

    [Fact]
    public void NoRom_ReadReturns0xFF()
    {
        var ar = new ActionReplay();
        Assert.Equal(0xFF, ar.ReadByte(0xF00000));
    }

    [Fact]
    public void Freeze_WithoutRom_NoOp()
    {
        var ar = new ActionReplay();
        ar.Freeze(); // Should not crash
        Assert.False(ar.FreezeRequested);
    }
}
