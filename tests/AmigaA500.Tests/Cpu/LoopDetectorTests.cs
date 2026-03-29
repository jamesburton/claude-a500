using AmigaA500.Core.Cpu;

namespace AmigaA500.Tests.Cpu;

public class LoopDetectorTests
{
    [Fact]
    public void DetectsTightLoop_SamePC()
    {
        var detector = new LoopDetector();
        bool detected = false;
        for (int i = 0; i < 50; i++)
        {
            if (detector.Track(0x1000)) { detected = true; break; }
        }
        Assert.True(detected);
    }

    [Fact]
    public void DetectsTightLoop_SmallRange()
    {
        var detector = new LoopDetector();
        // Alternate between two addresses (like SUBQ+BGT)
        for (int i = 0; i < 32; i++)
            detector.Track(i % 2 == 0 ? 0x1000u : 0x1002u);
        Assert.True(detector.Track(0x1000));
    }

    [Fact]
    public void NoFalsePositive_DifferentPCs()
    {
        var detector = new LoopDetector();
        for (int i = 0; i < 100; i++)
            Assert.False(detector.Track((uint)(i * 100)));
    }

    [Fact]
    public void AccelerateDbf_SetsToMinusOne()
    {
        uint reg = 0x0000FFFF & 0x1000; // D0 = 4096
        reg = 4096;
        int cycles = LoopDetector.AccelerateDbf(ref reg);
        Assert.Equal(0xFFFFu, reg & 0xFFFF); // Counter = -1
        Assert.True(cycles > 0);
    }

    [Fact]
    public void AccelerateDbf_AlreadyNegative()
    {
        uint reg = 0x0000FFFF; // Already -1
        int cycles = LoopDetector.AccelerateDbf(ref reg);
        Assert.Equal(0, cycles);
    }

    [Fact]
    public void Reset_ClearsState()
    {
        var detector = new LoopDetector();
        for (int i = 0; i < 40; i++)
            detector.Track(0x1000);
        detector.Reset();
        Assert.False(detector.Track(0x1000)); // Reset, not a loop yet
    }
}
