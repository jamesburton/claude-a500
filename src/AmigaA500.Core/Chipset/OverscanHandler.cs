namespace AmigaA500.Core.Chipset;

/// <summary>
/// Handles overscan display modes — extends the visible area beyond the standard window.
/// Standard PAL: 320x256, max overscan PAL: ~362x283.
/// </summary>
public sealed class OverscanHandler
{
    // Standard display area (PAL)
    public const int StdLeft = 0x81;   // DIWSTRT H
    public const int StdTop = 0x2C;    // DIWSTRT V
    public const int StdRight = 0x1C1; // DIWSTOP H
    public const int StdBottom = 0x12C; // DIWSTOP V (256+44)

    // Maximum overscan (PAL)
    public const int MaxLeft = 0x71;
    public const int MaxTop = 0x1A;
    public const int MaxRight = 0x1D1;
    public const int MaxBottom = 0x137;

    public int Left { get; set; } = StdLeft;
    public int Top { get; set; } = StdTop;
    public int Right { get; set; } = StdRight;
    public int Bottom { get; set; } = StdBottom;

    public int Width => Right - Left;
    public int Height => Bottom - Top;

    public void SetFromRegisters(ushort diwstrt, ushort diwstop)
    {
        Left = diwstrt & 0xFF;
        Top = (diwstrt >> 8) & 0xFF;
        Right = (diwstop & 0xFF) | 0x100; // Bit 8 implicit
        Bottom = ((diwstop >> 8) & 0xFF) | 0x100;
    }

    public bool IsOverscan => Left < StdLeft || Top < StdTop || Right > StdRight || Bottom > StdBottom;
    public bool IsStandard => Left == StdLeft && Top == StdTop && Right == StdRight && Bottom == StdBottom;

    /// <summary>
    /// Get the pixel offset within the overscan area for a given beam position.
    /// Returns (-1,-1) if outside the display window.
    /// </summary>
    public (int x, int y) BeamToPixel(int hpos, int vpos)
    {
        if (hpos < Left || hpos >= Right || vpos < Top || vpos >= Bottom)
            return (-1, -1);
        return (hpos - Left, vpos - Top);
    }
}
