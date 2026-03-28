namespace AmigaA500.Core.Chipset;

/// <summary>
/// Amiga Blitter — DMA-driven 2D graphics accelerator.
/// </summary>
public sealed class Blitter
{
    // Pointers
    public uint APt, BPt, CPt, DPt;
    // Modulos
    public short AMod, BMod, CMod, DMod;
    // Control
    public ushort BLTCON0, BLTCON1;
    // Masks
    public ushort FirstWordMask = 0xFFFF;
    public ushort LastWordMask = 0xFFFF;
    // Data registers
    public ushort ADat, BDat, CDat;

    // State
    public bool Busy { get; private set; }
    public bool Zero { get; private set; }

    // DMA access
    private readonly Func<uint, ushort> _dmaRead;
    private readonly Action<uint, ushort> _dmaWrite;
    private readonly Action? _onComplete;

    // Barrel shift pipeline
    private ushort _aOld, _bOld;

    public Blitter(Func<uint, ushort> dmaRead, Action<uint, ushort> dmaWrite, Action? onComplete = null)
    {
        _dmaRead = dmaRead;
        _dmaWrite = dmaWrite;
        _onComplete = onComplete;
    }

    public byte Minterm => (byte)(BLTCON0 & 0xFF);
    public bool UseA => (BLTCON0 & 0x0800) != 0;
    public bool UseB => (BLTCON0 & 0x0400) != 0;
    public bool UseC => (BLTCON0 & 0x0200) != 0;
    public bool UseD => (BLTCON0 & 0x0100) != 0;
    public int AShift => (BLTCON0 >> 12) & 0xF;
    public int BShift => (BLTCON1 >> 12) & 0xF;
    public bool IsLineMode => (BLTCON1 & 0x0001) != 0;
    public bool DescendingMode => (BLTCON1 & 0x0002) != 0;
    public bool InclusiveFill => (BLTCON1 & 0x0008) != 0;
    public bool ExclusiveFill => (BLTCON1 & 0x0010) != 0;

    /// <summary>
    /// Start a blit operation. Width (words) in low 6 bits, height in high 10 bits.
    /// </summary>
    public void Start(ushort bltsize)
    {
        int height = (bltsize >> 6) & 0x3FF;
        int width = bltsize & 0x3F;
        if (height == 0) height = 1024;
        if (width == 0) width = 64;

        Busy = true;
        Zero = true;
        _aOld = 0;
        _bOld = 0;

        if (IsLineMode)
            ExecuteLineDraw(height);
        else
            ExecuteAreaBlit(width, height);

        Busy = false;
        _onComplete?.Invoke();
    }

    private void ExecuteAreaBlit(int width, int height)
    {
        int step = DescendingMode ? -2 : 2;

        for (int y = 0; y < height; y++)
        {
            bool fillCarry = (BLTCON1 & 0x0004) != 0; // Fill carry input (FCI)

            for (int x = 0; x < width; x++)
            {
                ushort aWord = UseA ? _dmaRead(APt) : ADat;
                ushort bWord = UseB ? _dmaRead(BPt) : BDat;
                ushort cWord = UseC ? _dmaRead(CPt) : CDat;

                // Barrel shift A
                ushort aShifted = BarrelShift(aWord, _aOld, AShift);
                _aOld = aWord;

                // Barrel shift B
                ushort bShifted = BarrelShift(bWord, _bOld, BShift);
                _bOld = bWord;

                // Apply masks to A
                if (x == 0) aShifted &= FirstWordMask;
                if (x == width - 1) aShifted &= LastWordMask;

                // Apply minterm logic
                ushort result = ApplyMinterm(aShifted, bShifted, cWord, Minterm);

                // Apply fill if enabled
                if (InclusiveFill || ExclusiveFill)
                    result = ApplyFill(result, ref fillCarry, ExclusiveFill);

                if (result != 0) Zero = false;

                if (UseD) _dmaWrite(DPt, result);

                // Advance pointers
                if (UseA) APt = (uint)(APt + step);
                if (UseB) BPt = (uint)(BPt + step);
                if (UseC) CPt = (uint)(CPt + step);
                if (UseD) DPt = (uint)(DPt + step);
            }

            // Apply modulos at end of row
            if (UseA) APt = (uint)(APt + AMod);
            if (UseB) BPt = (uint)(BPt + BMod);
            if (UseC) CPt = (uint)(CPt + CMod);
            if (UseD) DPt = (uint)(DPt + DMod);

            // Reset shift pipeline at start of each row
            _aOld = 0;
            _bOld = 0;
        }
    }

    private void ExecuteLineDraw(int height)
    {
        // Bresenham line drawing mode
        // Simplified — basic implementation for common use cases
        // A channel provides texture pattern, C provides existing data, D outputs
        // APt holds error accumulator, AMod/BMod hold error increments
        short error = (short)APt; // Bresenham error term
        int octant = (BLTCON1 >> 2) & 0x7;

        for (int i = 0; i < height; i++)
        {
            ushort cWord = UseC ? _dmaRead(CPt) : CDat;
            ushort pattern = UseA ? ADat : (ushort)0xFFFF;

            // Determine bit position
            int bitPos = BShift;
            ushort pixel = (ushort)(pattern & (0x8000 >> bitPos));

            ushort result = ApplyMinterm(pixel != 0 ? (ushort)0xFFFF : (ushort)0, (ushort)(1 << (15 - bitPos)), cWord, Minterm);
            if (result != 0) Zero = false;

            if (UseD) _dmaWrite(DPt, result);

            // Bresenham step
            if (error >= 0)
            {
                error += (short)AMod; // Diagonal step
                // Move in minor axis (depends on octant)
            }
            else
            {
                error += (short)BMod; // Straight step
            }
        }
    }

    public static ushort ApplyMinterm(ushort a, ushort b, ushort c, byte minterm)
    {
        ushort result = 0;
        if ((minterm & 0x80) != 0) result |= (ushort)(a & b & c);
        if ((minterm & 0x40) != 0) result |= (ushort)(a & b & ~c);
        if ((minterm & 0x20) != 0) result |= (ushort)(a & ~b & c);
        if ((minterm & 0x10) != 0) result |= (ushort)(a & ~b & ~c);
        if ((minterm & 0x08) != 0) result |= (ushort)(~a & b & c);
        if ((minterm & 0x04) != 0) result |= (ushort)(~a & b & ~c);
        if ((minterm & 0x02) != 0) result |= (ushort)(~a & ~b & c);
        if ((minterm & 0x01) != 0) result |= (ushort)(~a & ~b & ~c);
        return result;
    }

    private static ushort BarrelShift(ushort current, ushort previous, int shift)
    {
        if (shift == 0) return current;
        uint combined = ((uint)previous << 16) | current;
        return (ushort)(combined >> shift);
    }

    private static ushort ApplyFill(ushort data, ref bool carry, bool exclusive)
    {
        ushort result = 0;
        // Process bits right to left (bit 0 first)
        for (int bit = 0; bit < 16; bit++)
        {
            bool srcBit = (data & (1 << bit)) != 0;

            if (exclusive)
            {
                if (srcBit) carry = !carry;
                if (carry) result |= (ushort)(1 << bit);
            }
            else // Inclusive
            {
                if (srcBit) carry = !carry;
                if (carry || srcBit) result |= (ushort)(1 << bit);
            }
        }
        return result;
    }
}
