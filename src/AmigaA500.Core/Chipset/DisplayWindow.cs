namespace AmigaA500.Core.Chipset;

/// <summary>
/// Display window management — DIWSTRT, DIWSTOP, DDFSTRT, DDFSTOP.
/// </summary>
public sealed class DisplayWindow
{
    // Display window (visible area)
    public ushort DIWSTRT = 0x2C81; // Default: V=$2C (line 44), H=$81 (position 129)
    public ushort DIWSTOP = 0xF4C1; // Default: V=$F4 (line 244), H=$C1 (position 193)

    // Data fetch window (DMA fetch area)
    public ushort DDFSTRT = 0x0038; // Default: $38 (lores)
    public ushort DDFSTOP = 0x00D0; // Default: $D0 (lores)

    public int DisplayStartV => (DIWSTRT >> 8) & 0xFF;
    public int DisplayStartH => DIWSTRT & 0xFF;
    public int DisplayStopV => ((DIWSTOP >> 8) & 0xFF) | 0x100; // Bit 8 is implicit
    public int DisplayStopH => (DIWSTOP & 0xFF) | 0x100;

    public int FetchStartH => DDFSTRT & 0xFF;
    public int FetchStopH => DDFSTOP & 0xFF;

    public int VisibleWidth => DisplayStopH - DisplayStartH;
    public int VisibleHeight => DisplayStopV - DisplayStartV;

    public bool IsInDisplayWindow(int hpos, int vpos)
    {
        return hpos >= DisplayStartH && hpos < DisplayStopH &&
               vpos >= DisplayStartV && vpos < DisplayStopV;
    }

    public bool IsInFetchWindow(int hpos)
    {
        return hpos >= FetchStartH && hpos <= FetchStopH;
    }
}
