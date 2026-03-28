using AmigaA500.Core.Chipset;

namespace AmigaA500.Tests.Chipset;

public class AgnusTests
{
    private readonly byte[] _mem = new byte[65536];
    private readonly CustomRegisters _regs = new();
    private Agnus CreateAgnus() => new(_regs, addr => (ushort)(_mem[addr] << 8 | _mem[addr + 1]), (addr, val) => { _mem[addr] = (byte)(val >> 8); _mem[addr + 1] = (byte)val; });

    [Fact]
    public void BeamCounter_AdvancesHorizontal()
    {
        var agnus = CreateAgnus();
        agnus.AdvanceClock();
        Assert.Equal(1, agnus.HPos);
    }

    [Fact]
    public void BeamCounter_WrapsToNextLine()
    {
        var agnus = CreateAgnus();
        for (int i = 0; i < Agnus.ColorClocksPerLine; i++)
            agnus.AdvanceClock();
        Assert.Equal(0, agnus.HPos);
        Assert.Equal(1, agnus.VPos);
    }

    [Fact]
    public void BeamCounter_VblankAtEndOfFrame()
    {
        var agnus = CreateAgnus();
        bool vblank = false;
        for (int i = 0; i < Agnus.ColorClocksPerLine * Agnus.LinesPerFramePal; i++)
            vblank = agnus.AdvanceClock();
        Assert.True(vblank);
        Assert.Equal(0, agnus.VPos);
    }

    [Fact]
    public void VPOSR_ReturnsBeamPosition()
    {
        var agnus = CreateAgnus();
        for (int i = 0; i < Agnus.ColorClocksPerLine * 100; i++)
            agnus.AdvanceClock();
        ushort vhpos = agnus.ReadVHPOSR();
        Assert.Equal(100, (vhpos >> 8) & 0xFF);
    }

    [Fact]
    public void DmaEnabled_RequiresMaster()
    {
        var agnus = CreateAgnus();
        // No DMA enabled
        Assert.False(agnus.DmaEnabled(DmaChannel.Bitplane));

        // Enable master + bitplane
        _regs.WriteRegister(0x096, 0x8300); // SET + DMAEN + BPLEN
        Assert.True(agnus.DmaEnabled(DmaChannel.Bitplane));
    }

    [Fact]
    public void DmaEnabled_IndividualChannels()
    {
        var agnus = CreateAgnus();
        _regs.WriteRegister(0x096, 0x8201); // Master + AUD0
        Assert.True(agnus.DmaEnabled(DmaChannel.Aud0));
        Assert.False(agnus.DmaEnabled(DmaChannel.Aud1));
    }

    [Fact]
    public void LongFrame_TogglesEachFrame()
    {
        var agnus = CreateAgnus();
        Assert.False(agnus.LongFrame);

        // Run one full frame
        for (int i = 0; i < Agnus.ColorClocksPerLine * Agnus.LinesPerFramePal; i++)
            agnus.AdvanceClock();
        Assert.True(agnus.LongFrame);

        // Run another frame
        for (int i = 0; i < Agnus.ColorClocksPerLine * Agnus.LinesPerFramePal; i++)
            agnus.AdvanceClock();
        Assert.False(agnus.LongFrame);
    }
}
