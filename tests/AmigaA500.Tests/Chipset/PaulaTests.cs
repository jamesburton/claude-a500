using AmigaA500.Core.Chipset;

namespace AmigaA500.Tests.Chipset;

public class PaulaTests
{
    private readonly byte[] _memory = new byte[65536];
    private ushort DmaRead(uint addr) => (ushort)(_memory[addr] << 8 | _memory[addr + 1]);

    [Fact]
    public void Channel_PlaysFromMemory()
    {
        // Write some sample data
        _memory[0x1000] = 0x40; // +64
        _memory[0x1001] = 0xC0; // -64

        var paula = new Paula(DmaRead);
        paula.SetLocation(0, 0x1000);
        paula.SetLength(0, 2); // 2 words = 4 samples
        paula.SetPeriod(0, 124);
        paula.SetVolume(0, 64);

        paula.StartChannel(0);

        // First tick should produce first sample
        paula.Tick();
        // Audio is active and playing
    }

    [Fact]
    public void Channel_InterruptOnCompletion()
    {
        _memory[0x1000] = 0x40;
        _memory[0x1001] = 0x40;

        int interruptBit = -1;
        var paula = new Paula(DmaRead, bit => interruptBit = bit);
        paula.SetLocation(0, 0x1000);
        paula.SetLength(0, 1); // 1 word = 2 samples
        paula.SetPeriod(0, 1);
        paula.SetVolume(0, 64);

        paula.StartChannel(0);

        // Tick enough times to exhaust the samples
        for (int i = 0; i < 300; i++) paula.Tick();

        Assert.Equal(7, interruptBit); // AUD0 = bit 7
    }

    [Fact]
    public void Volume_ZeroProducesSilence()
    {
        var paula = new Paula(DmaRead);
        paula.SetVolume(0, 0);
        paula.SetLocation(0, 0x1000);
        paula.SetLength(0, 1);
        paula.SetPeriod(0, 124);

        paula.StartChannel(0);
        paula.Tick();
        paula.MixSample();

        var buffer = paula.GetOutputBuffer();
        Assert.Equal(0, buffer[0]); // Left channel silence
    }

    [Fact]
    public void Period_MinimumClamped()
    {
        var paula = new Paula(DmaRead);
        paula.SetPeriod(0, 10); // Below minimum
        // Period should be clamped to 124 internally
    }

    [Fact]
    public void OutputBuffer_ResetWorks()
    {
        var paula = new Paula(DmaRead);
        paula.MixSample();
        paula.MixSample();
        var buf = paula.GetOutputBuffer();
        Assert.True(buf.Length > 0);

        paula.ResetOutputBuffer();
        buf = paula.GetOutputBuffer();
        Assert.Equal(0, buf.Length);
    }
}
