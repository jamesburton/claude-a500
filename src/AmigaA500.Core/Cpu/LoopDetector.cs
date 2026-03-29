namespace AmigaA500.Core.Cpu;

/// <summary>
/// Detects tight CPU loops and optionally accelerates them.
/// Common patterns: DBcc loops, SUBQ+Bcc loops used for delays and memory sizing.
/// </summary>
public sealed class LoopDetector
{
    private uint _loopBasePC;
    private int _repeatCount;
    private const int LoopThreshold = 32;

    /// <summary>
    /// Track PC changes. Returns true if a tight loop is detected.
    /// Detects loops where PC stays within a small range (e.g., 2-instruction loops).
    /// </summary>
    public bool Track(uint pc)
    {
        // Check if PC is within 8 bytes of the loop base (covers 2-4 instruction loops)
        if (_loopBasePC != 0 && pc >= _loopBasePC - 8 && pc <= _loopBasePC + 8)
        {
            _repeatCount++;
            return _repeatCount >= LoopThreshold;
        }
        _loopBasePC = pc;
        _repeatCount = 0;
        return false;
    }

    /// <summary>
    /// Accelerate a detected DBcc loop by predicting the exit condition.
    /// For DBF (DBcc with false condition), the loop runs until counter = -1.
    /// </summary>
    public static int AccelerateDbf(ref uint counterReg)
    {
        // DBF decrements word portion and loops until -1
        int remaining = (short)(counterReg & 0xFFFF);
        if (remaining < 0) return 0;

        int cycles = (remaining + 1) * 10; // ~10 cycles per DBF iteration
        counterReg = (counterReg & 0xFFFF0000) | 0xFFFF; // Set to -1 (exit condition)
        return cycles;
    }

    /// <summary>
    /// Accelerate a SUBQ+BGT delay loop.
    /// </summary>
    public static int AccelerateSubqBgt(ref uint counterReg)
    {
        int value = (int)counterReg;
        if (value <= 0) return 0;

        int cycles = value * 12; // ~12 cycles per SUBQ+BGT iteration
        counterReg = 0;
        return cycles;
    }

    public void Reset()
    {
        _loopBasePC = 0;
        _repeatCount = 0;
    }
}
