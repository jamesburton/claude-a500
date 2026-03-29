namespace AmigaA500.Core.Chipset;

/// <summary>
/// Standalone beam counter with PAL/NTSC timing and event generation.
/// </summary>
public sealed class BeamCounter
{
    public int HPos { get; private set; }
    public int VPos { get; private set; }
    public bool LongFrame { get; private set; }
    public bool IsPal { get; set; } = true;

    public int MaxH => 227;
    public int MaxV => IsPal ? 312 : 262;

    public event Action? OnVBlank;
    public event Action? OnHBlank;
    public event Action<int>? OnLineStart;

    public bool Tick()
    {
        HPos++;
        if (HPos >= MaxH)
        {
            HPos = 0;
            OnHBlank?.Invoke();
            VPos++;
            OnLineStart?.Invoke(VPos);

            if (VPos >= MaxV)
            {
                VPos = 0;
                LongFrame = !LongFrame;
                OnVBlank?.Invoke();
                return true;
            }
        }
        return false;
    }

    public bool InDisplayArea => VPos >= 26 && VPos < 26 + 256 && HPos >= 64 && HPos < 64 + 160;
    public int DisplayLine => VPos - 26;
    public int DisplayColumn => (HPos - 64) * 2;

    public void Reset()
    {
        HPos = 0;
        VPos = 0;
        LongFrame = false;
    }
}
