namespace AmigaA500.Core.Chipset;

/// <summary>
/// Amiga audio output filter chain.
/// The A500 has a fixed low-pass filter (~4.5 kHz) on the audio output,
/// plus an optional LED filter (~3.3 kHz) controlled by CIA-A port A bit 1.
/// </summary>
public sealed class AudioFilter
{
    private double _prevLeft, _prevRight;
    private double _prevLedLeft, _prevLedRight;
    private bool _ledFilterEnabled;
    private readonly double _sampleRate;

    // RC filter coefficients
    private readonly double _fixedAlpha;  // ~4.5 kHz fixed filter
    private readonly double _ledAlpha;    // ~3.3 kHz LED filter

    public AudioFilter(int sampleRate = 44100)
    {
        _sampleRate = sampleRate;
        // RC low-pass: alpha = dt / (RC + dt), where RC = 1/(2*pi*fc)
        _fixedAlpha = ComputeAlpha(4500, sampleRate);
        _ledAlpha = ComputeAlpha(3300, sampleRate);
    }

    public bool LedFilterEnabled
    {
        get => _ledFilterEnabled;
        set => _ledFilterEnabled = value;
    }

    /// <summary>
    /// Apply the Amiga audio filter chain to a stereo sample pair.
    /// </summary>
    public (short left, short right) Filter(short left, short right)
    {
        double l = left;
        double r = right;

        // Fixed low-pass filter (always active)
        l = _prevLeft + _fixedAlpha * (l - _prevLeft);
        r = _prevRight + _fixedAlpha * (r - _prevRight);
        _prevLeft = l;
        _prevRight = r;

        // LED filter (active when power LED is dimmed)
        if (_ledFilterEnabled)
        {
            l = _prevLedLeft + _ledAlpha * (l - _prevLedLeft);
            r = _prevLedRight + _ledAlpha * (r - _prevLedRight);
            _prevLedLeft = l;
            _prevLedRight = r;
        }

        return ((short)Math.Clamp(l, short.MinValue, short.MaxValue),
                (short)Math.Clamp(r, short.MinValue, short.MaxValue));
    }

    /// <summary>
    /// Apply filter to an entire buffer of interleaved stereo samples.
    /// </summary>
    public void FilterBuffer(Span<short> buffer)
    {
        for (int i = 0; i < buffer.Length - 1; i += 2)
        {
            var (l, r) = Filter(buffer[i], buffer[i + 1]);
            buffer[i] = l;
            buffer[i + 1] = r;
        }
    }

    public void Reset()
    {
        _prevLeft = _prevRight = 0;
        _prevLedLeft = _prevLedRight = 0;
    }

    private static double ComputeAlpha(double cutoffHz, double sampleRate)
    {
        double rc = 1.0 / (2.0 * Math.PI * cutoffHz);
        double dt = 1.0 / sampleRate;
        return dt / (rc + dt);
    }
}
