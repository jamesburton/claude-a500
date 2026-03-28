namespace AmigaA500.Core.Chipset;

/// <summary>
/// Paula audio subsystem — 4-channel 8-bit PCM with DMA.
/// </summary>
public sealed class Paula
{
    public const int NumChannels = 4;
    public const int SampleRate = 44100; // Host output rate
    private const double AmigaClockPal = 3_546_895.0;

    private readonly AudioChannel[] _channels = new AudioChannel[NumChannels];
    private readonly Func<uint, ushort> _dmaRead;
    private readonly Action<int>? _onInterrupt;

    // Audio output buffer
    private readonly short[] _outputBuffer;
    private int _outputPos;

    public Paula(Func<uint, ushort> dmaRead, Action<int>? onInterrupt = null, int bufferSize = 4096)
    {
        _dmaRead = dmaRead;
        _onInterrupt = onInterrupt;
        _outputBuffer = new short[bufferSize];

        for (int i = 0; i < NumChannels; i++)
            _channels[i] = new AudioChannel();
    }

    public void SetLocation(int ch, uint addr) => _channels[ch].Location = addr;
    public void SetLength(int ch, ushort len) => _channels[ch].Length = len;
    public void SetPeriod(int ch, ushort per) => _channels[ch].Period = Math.Max(per, (ushort)124);
    public void SetVolume(int ch, ushort vol) => _channels[ch].Volume = Math.Min(vol, (ushort)64);
    public void SetData(int ch, ushort dat) => _channels[ch].Data = dat;

    /// <summary>
    /// Called each DMA slot to advance audio channels.
    /// </summary>
    public void Tick()
    {
        for (int ch = 0; ch < NumChannels; ch++)
        {
            var c = _channels[ch];
            if (!c.Active) continue;

            c.PeriodCounter--;
            if (c.PeriodCounter <= 0)
            {
                c.PeriodCounter = c.Period;

                // Get current sample byte
                sbyte sample;
                if (c.UseHigh)
                {
                    sample = (sbyte)(c.Data >> 8);
                    c.UseHigh = false;
                }
                else
                {
                    sample = (sbyte)(c.Data & 0xFF);
                    c.UseHigh = true;

                    // Fetch next word via DMA
                    c.Data = _dmaRead(c.WorkingPtr);
                    c.WorkingPtr += 2;
                    c.WorkingLength--;

                    if (c.WorkingLength == 0)
                    {
                        // Reload from location registers
                        c.WorkingPtr = c.Location;
                        c.WorkingLength = c.Length;
                        // Trigger audio interrupt
                        _onInterrupt?.Invoke(7 + ch); // AUD0=bit7, AUD1=bit8, etc.
                    }
                }

                c.CurrentSample = sample;
            }
        }
    }

    /// <summary>
    /// Mix all channels and write to output buffer. Returns true when buffer is full.
    /// </summary>
    public bool MixSample()
    {
        // Left channel: ch0 + ch3
        // Right channel: ch1 + ch2
        int left = MixChannel(0) + MixChannel(3);
        int right = MixChannel(1) + MixChannel(2);

        // Clamp to 16-bit range
        left = Math.Clamp(left, short.MinValue, short.MaxValue);
        right = Math.Clamp(right, short.MinValue, short.MaxValue);

        if (_outputPos + 1 < _outputBuffer.Length)
        {
            _outputBuffer[_outputPos++] = (short)left;
            _outputBuffer[_outputPos++] = (short)right;
        }

        return _outputPos >= _outputBuffer.Length;
    }

    private int MixChannel(int ch)
    {
        var c = _channels[ch];
        if (!c.Active) return 0;
        return c.CurrentSample * c.Volume * 4; // Scale to ~16-bit range
    }

    public ReadOnlySpan<short> GetOutputBuffer() => _outputBuffer.AsSpan(0, _outputPos);
    public void ResetOutputBuffer() => _outputPos = 0;

    public void EnableChannel(int ch) => _channels[ch].Active = true;
    public void DisableChannel(int ch) => _channels[ch].Active = false;

    public void StartChannel(int ch)
    {
        var c = _channels[ch];
        c.WorkingPtr = c.Location;
        c.WorkingLength = c.Length;
        c.PeriodCounter = c.Period;
        c.UseHigh = true;
        c.Active = true;

        // Initial DMA fetch
        c.Data = _dmaRead(c.WorkingPtr);
        c.WorkingPtr += 2;
    }

    private class AudioChannel
    {
        public uint Location;       // AUDxLC
        public ushort Length;        // AUDxLEN (in words)
        public ushort Period = 124;  // AUDxPER (min 124)
        public ushort Volume = 64;   // AUDxVOL (0-64)
        public ushort Data;          // AUDxDAT

        public uint WorkingPtr;
        public ushort WorkingLength;
        public int PeriodCounter;
        public bool UseHigh;
        public bool Active;
        public sbyte CurrentSample;
    }
}
