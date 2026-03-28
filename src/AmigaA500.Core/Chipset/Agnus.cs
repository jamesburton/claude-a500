namespace AmigaA500.Core.Chipset;

/// <summary>
/// Agnus — DMA controller and bus arbiter. Manages beam counters and DMA scheduling.
/// </summary>
public sealed class Agnus
{
    public const int LinesPerFramePal = 312;
    public const int LinesPerFrameNtsc = 262;
    public const int ColorClocksPerLine = 227;

    private readonly CustomRegisters _regs;
    private readonly Func<uint, ushort> _dmaRead;
    private readonly Action<uint, ushort> _dmaWrite;

    public int HPos { get; private set; }
    public int VPos { get; private set; }
    public bool LongFrame { get; private set; }
    public bool IsPal { get; set; } = true;

    public int LinesPerFrame => IsPal ? LinesPerFramePal : LinesPerFrameNtsc;

    public Agnus(CustomRegisters regs, Func<uint, ushort> dmaRead, Action<uint, ushort> dmaWrite)
    {
        _regs = regs;
        _dmaRead = dmaRead;
        _dmaWrite = dmaWrite;
    }

    public ushort ReadVPOSR()
    {
        // Bit 15: LOF (long frame for interlace)
        // Bit 0: V8 (bit 8 of vertical position)
        return (ushort)((LongFrame ? 0x8000 : 0) | ((VPos >> 8) & 1));
    }

    public ushort ReadVHPOSR()
    {
        return (ushort)(((VPos & 0xFF) << 8) | (HPos & 0xFF));
    }

    /// <summary>
    /// Advance one color clock. Returns true at VBLANK.
    /// </summary>
    public bool AdvanceClock()
    {
        HPos++;
        bool vblank = false;

        if (HPos >= ColorClocksPerLine)
        {
            HPos = 0;
            VPos++;

            if (VPos >= LinesPerFrame)
            {
                VPos = 0;
                LongFrame = !LongFrame;
                vblank = true;
            }
        }

        return vblank;
    }

    /// <summary>
    /// Check if DMA is enabled for a specific channel.
    /// </summary>
    public bool DmaEnabled(DmaChannel channel)
    {
        ushort dmacon = _regs.DMACON;
        if ((dmacon & 0x0200) == 0) return false; // Master DMA enable
        return (dmacon & (1 << (int)channel)) != 0;
    }

    /// <summary>
    /// Execute bitplane DMA for current line.
    /// </summary>
    public void FetchBitplanes()
    {
        if (!DmaEnabled(DmaChannel.Bitplane)) return;

        int numBpl = (_regs.BPLCON0 >> 12) & 7;
        if (numBpl > 6) numBpl = 6;

        for (int i = 0; i < numBpl; i++)
        {
            ushort data = _dmaRead(_regs.BplPt[i]);
            _regs.BplPt[i] += 2;
            // Data would be loaded into Denise shift registers
        }

        // Apply modulos at end of line
        for (int i = 0; i < numBpl; i++)
        {
            if ((i & 1) == 0)
                _regs.BplPt[i] = (uint)(_regs.BplPt[i] + _regs.BPL1MOD);
            else
                _regs.BplPt[i] = (uint)(_regs.BplPt[i] + _regs.BPL2MOD);
        }
    }

    public void Reset()
    {
        HPos = 0;
        VPos = 0;
        LongFrame = false;
    }
}

public enum DmaChannel
{
    Aud0 = 0, Aud1 = 1, Aud2 = 2, Aud3 = 3,
    Disk = 4, Sprite = 5, Blitter = 6, Copper = 7,
    Bitplane = 8, Master = 9
}
