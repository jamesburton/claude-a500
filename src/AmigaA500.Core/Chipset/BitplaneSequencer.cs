namespace AmigaA500.Core.Chipset;

/// <summary>
/// Bitplane DMA sequencer — manages the fetch and shift pipeline
/// for converting DMA data into pixel indices.
/// </summary>
public sealed class BitplaneSequencer
{
    // Shift registers (one per bitplane)
    private readonly ushort[] _shiftReg = new ushort[6];
    // Pending data (loaded from DMA, transferred to shift on next fetch boundary)
    private readonly ushort[] _pending = new ushort[6];
    private bool _hasPending;

    public int NumBitplanes { get; set; }
    public bool Hires { get; set; }

    /// <summary>
    /// Load DMA data for a bitplane. Called during DMA fetch slots.
    /// </summary>
    public void LoadData(int plane, ushort data)
    {
        if (plane >= 0 && plane < 6)
        {
            _pending[plane] = data;
            _hasPending = true;
        }
    }

    /// <summary>
    /// Transfer pending data to shift registers. Called at fetch boundary.
    /// </summary>
    public void TransferToShift()
    {
        if (!_hasPending) return;
        for (int i = 0; i < 6; i++)
            _shiftReg[i] = _pending[i];
        _hasPending = false;
    }

    /// <summary>
    /// Get pixel index by shifting out bits from all active bitplanes.
    /// </summary>
    public int ShiftPixel()
    {
        int index = 0;
        int n = Math.Min(NumBitplanes, 6);
        for (int i = 0; i < n; i++)
        {
            index |= ((_shiftReg[i] >> 15) & 1) << i;
            _shiftReg[i] <<= 1;
        }
        return index;
    }

    /// <summary>
    /// Get pixel index with scroll delay applied.
    /// </summary>
    public int ShiftPixelWithDelay(int delay)
    {
        // Delay is applied by shifting additional positions at line start
        // For simplicity, just shift normally
        return ShiftPixel();
    }

    public void Reset()
    {
        Array.Clear(_shiftReg);
        Array.Clear(_pending);
        _hasPending = false;
    }
}
