using AmigaA500.Core.Chipset;

namespace AmigaA500.Tests.Chipset;

public class AudioFilterTests
{
    [Fact]
    public void Filter_AttenuatesHighFrequency()
    {
        var filter = new AudioFilter(44100);
        // Feed alternating +/- samples (high frequency)
        short prev = 0;
        for (int i = 0; i < 100; i++)
        {
            short input = (short)(i % 2 == 0 ? 10000 : -10000);
            var (l, _) = filter.Filter(input, input);
            prev = l;
        }
        // After filtering, amplitude should be reduced
        Assert.True(Math.Abs(prev) < 10000);
    }

    [Fact]
    public void Filter_PassesLowFrequency()
    {
        var filter = new AudioFilter(44100);
        // Feed constant signal (DC = 0 Hz)
        for (int i = 0; i < 1000; i++)
            filter.Filter(5000, 5000);

        var (l, r) = filter.Filter(5000, 5000);
        // Should converge close to input
        Assert.True(Math.Abs(l - 5000) < 500);
        Assert.True(Math.Abs(r - 5000) < 500);
    }

    [Fact]
    public void LedFilter_FurtherAttenuates()
    {
        var filter = new AudioFilter(44100);
        filter.LedFilterEnabled = true;

        short withLed = 0;
        for (int i = 0; i < 100; i++)
        {
            short input = (short)(i % 2 == 0 ? 10000 : -10000);
            var (l, _) = filter.Filter(input, input);
            withLed = l;
        }

        filter.Reset();
        filter.LedFilterEnabled = false;
        short withoutLed = 0;
        for (int i = 0; i < 100; i++)
        {
            short input = (short)(i % 2 == 0 ? 10000 : -10000);
            var (l, _) = filter.Filter(input, input);
            withoutLed = l;
        }

        // LED filter should attenuate more
        Assert.True(Math.Abs(withLed) <= Math.Abs(withoutLed));
    }

    [Fact]
    public void FilterBuffer_ProcessesPairs()
    {
        var filter = new AudioFilter(44100);
        var buffer = new short[] { 10000, -10000, 10000, -10000, 10000, -10000 };
        filter.FilterBuffer(buffer);
        // Buffer should be modified (filtered)
        Assert.NotEqual(10000, buffer[0]); // First sample gets filtered
    }

    [Fact]
    public void Reset_ClearsState()
    {
        var filter = new AudioFilter(44100);
        filter.Filter(10000, 10000);
        filter.Reset();
        var (l, r) = filter.Filter(0, 0);
        Assert.Equal(0, l);
        Assert.Equal(0, r);
    }
}
