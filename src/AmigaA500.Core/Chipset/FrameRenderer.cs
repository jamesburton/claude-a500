namespace AmigaA500.Core.Chipset;

/// <summary>
/// Renders a complete PAL frame to the framebuffer.
/// Combines bitplane data, sprites, and colors into 32-bit RGBA output.
/// </summary>
public sealed class FrameRenderer
{
    public const int Width = 320;
    public const int Height = 256;
    public const int Stride = Width;

    private readonly Denise _denise;
    private readonly SpriteEngine _sprites;
    private readonly uint[] _framebuffer;

    public FrameRenderer(Denise denise, SpriteEngine sprites, uint[] framebuffer)
    {
        _denise = denise;
        _sprites = sprites;
        _framebuffer = framebuffer;
    }

    /// <summary>
    /// Render a single scanline to the framebuffer.
    /// </summary>
    public void RenderLine(int line, ushort[] bitplaneData0, ushort[] bitplaneData1,
                           ushort[] bitplaneData2, ushort[] bitplaneData3,
                           ushort[] bitplaneData4, ushort[] bitplaneData5)
    {
        if (line < 0 || line >= Height) return;

        _denise.StartLine();
        int offset = line * Stride;

        for (int word = 0; word < Width / 16; word++)
        {
            // Load bitplane data for this 16-pixel group
            if (bitplaneData0 != null && word < bitplaneData0.Length) _denise.LoadBitplaneData(0, bitplaneData0[word]);
            if (bitplaneData1 != null && word < bitplaneData1.Length) _denise.LoadBitplaneData(1, bitplaneData1[word]);
            if (bitplaneData2 != null && word < bitplaneData2.Length) _denise.LoadBitplaneData(2, bitplaneData2[word]);
            if (bitplaneData3 != null && word < bitplaneData3.Length) _denise.LoadBitplaneData(3, bitplaneData3[word]);
            if (bitplaneData4 != null && word < bitplaneData4.Length) _denise.LoadBitplaneData(4, bitplaneData4[word]);
            if (bitplaneData5 != null && word < bitplaneData5.Length) _denise.LoadBitplaneData(5, bitplaneData5[word]);

            for (int pixel = 0; pixel < 16; pixel++)
            {
                int x = word * 16 + pixel;
                if (x >= Width) break;

                // Get playfield color
                ushort color = _denise.GetPixelColor(pixel);

                // Check sprites
                int? spriteColor = _sprites.GetSpritePixel(x, line);
                if (spriteColor.HasValue)
                    color = _denise.GetColor(spriteColor.Value);

                _framebuffer[offset + x] = Denise.Rgb12ToRgba32(color);
            }
        }
    }

    /// <summary>
    /// Fill entire framebuffer with background color.
    /// </summary>
    public void ClearScreen()
    {
        uint bg = Denise.Rgb12ToRgba32(_denise.GetColor(0));
        Array.Fill(_framebuffer, bg, 0, Width * Height);
    }
}
