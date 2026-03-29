namespace AmigaA500.Core.Chipset;

/// <summary>
/// Mixes 4 Amiga audio channels to stereo PCM output with linear resampling.
/// The Amiga routes channels 0 and 3 to the left speaker, 1 and 2 to the right.
/// Input is 8-bit signed samples at the Amiga clock rate; output is 16-bit stereo
/// at the host sample rate (default 44100 Hz).
/// </summary>
public sealed class AudioMixer
{
    private const double AmigaClockPal = 3_546_895.0;

    private readonly int _outputRate;
    private readonly double _clocksPerSample;
    private double _clockAccumulator;

    // Per-channel state (left: 0, 3 / right: 1, 2)
    private readonly sbyte[] _samples = new sbyte[4];
    private readonly short[] _outputBuffer;
    private int _outputPos;

    /// <summary>
    /// Initialise the mixer.
    /// </summary>
    /// <param name="outputRate">Host audio sample rate in Hz (default 44100).</param>
    /// <param name="bufferSamples">Number of stereo frames in the output buffer (default 2048).</param>
    public AudioMixer(int outputRate = 44100, int bufferSamples = 2048)
    {
        _outputRate = outputRate;
        _clocksPerSample = AmigaClockPal / outputRate;
        _outputBuffer = new short[bufferSamples * 2]; // interleaved L/R
    }

    /// <summary>
    /// Push one sample for the given channel (0–3). Called by Paula each time a channel
    /// produces a new audio sample.
    /// </summary>
    public void SetChannelSample(int channel, sbyte sample)
    {
        if ((uint)channel < 4)
            _samples[channel] = sample;
    }

    /// <summary>
    /// Advance the mixer by one Amiga clock tick. When enough clocks have accumulated
    /// for the next output sample, a stereo frame is written to the internal buffer.
    /// Returns true when the output buffer is full.
    /// </summary>
    public bool Tick()
    {
        _clockAccumulator++;

        if (_clockAccumulator < _clocksPerSample)
            return false;

        _clockAccumulator -= _clocksPerSample;
        return WriteStereoFrame();
    }

    /// <summary>
    /// Advance the mixer by <paramref name="clocks"/> Amiga clock ticks, writing as
    /// many output samples as needed. Returns true if the buffer became full during
    /// this call.
    /// </summary>
    public bool AdvanceClocks(int clocks)
    {
        _clockAccumulator += clocks;
        bool full = false;

        while (_clockAccumulator >= _clocksPerSample)
        {
            _clockAccumulator -= _clocksPerSample;
            if (WriteStereoFrame())
                full = true;
        }

        return full;
    }

    private bool WriteStereoFrame()
    {
        if (_outputPos + 1 >= _outputBuffer.Length)
            return true; // Buffer already full

        // Left: channels 0 + 3
        int left = (_samples[0] + _samples[3]) * 256;
        // Right: channels 1 + 2
        int right = (_samples[1] + _samples[2]) * 256;

        _outputBuffer[_outputPos++] = (short)Math.Clamp(left, short.MinValue, short.MaxValue);
        _outputBuffer[_outputPos++] = (short)Math.Clamp(right, short.MinValue, short.MaxValue);

        return _outputPos >= _outputBuffer.Length;
    }

    /// <summary>
    /// Returns the samples written since the last <see cref="Reset"/> call.
    /// The span contains interleaved left/right 16-bit samples.
    /// </summary>
    public ReadOnlySpan<short> GetBuffer() => _outputBuffer.AsSpan(0, _outputPos);

    /// <summary>Number of complete stereo frames written to the buffer.</summary>
    public int FramesReady => _outputPos / 2;

    /// <summary>
    /// Reset the output buffer position to zero without clearing the data.
    /// </summary>
    public void Reset() => _outputPos = 0;

    /// <summary>Host output sample rate in Hz.</summary>
    public int OutputRate => _outputRate;

    /// <summary>Amiga clock ticks required per output sample.</summary>
    public double ClocksPerSample => _clocksPerSample;
}
