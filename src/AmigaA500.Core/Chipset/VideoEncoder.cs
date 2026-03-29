namespace AmigaA500.Core.Chipset;

/// <summary>
/// Converts the Amiga framebuffer to standard video output formats.
/// Supports PAL/NTSC timing, scanline effects, and aspect ratio correction.
/// </summary>
public sealed class VideoEncoder
{
    public const int PalWidth = 320;
    public const int PalHeight = 256;
    public const int NtscHeight = 200;
    public const int HiresWidth = 640;

    /// <summary>
    /// Scale the 320x256 framebuffer to the target resolution with optional scanlines.
    /// </summary>
    public static void ScaleFramebuffer(ReadOnlySpan<uint> source, Span<uint> dest,
        int srcW, int srcH, int dstW, int dstH, bool scanlines = false)
    {
        double xRatio = (double)srcW / dstW;
        double yRatio = (double)srcH / dstH;

        for (int y = 0; y < dstH; y++)
        {
            int srcY = (int)(y * yRatio);
            if (srcY >= srcH) srcY = srcH - 1;

            bool isScanline = scanlines && (y % 2 == 1);

            for (int x = 0; x < dstW; x++)
            {
                int srcX = (int)(x * xRatio);
                if (srcX >= srcW) srcX = srcW - 1;

                uint pixel = source[srcY * srcW + srcX];

                if (isScanline)
                {
                    // Darken scanline rows by 50%
                    uint r = ((pixel >> 16) & 0xFF) / 2;
                    uint g = ((pixel >> 8) & 0xFF) / 2;
                    uint b = (pixel & 0xFF) / 2;
                    pixel = 0xFF000000 | (r << 16) | (g << 8) | b;
                }

                dest[y * dstW + x] = pixel;
            }
        }
    }

    /// <summary>
    /// Apply PAL color blending (simulates the chroma subsampling of real PAL output).
    /// </summary>
    public static void ApplyPalBlending(Span<uint> framebuffer, int width, int height)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 1; x < width; x++)
            {
                int idx = y * width + x;
                int prevIdx = idx - 1;

                uint curr = framebuffer[idx];
                uint prev = framebuffer[prevIdx];

                // Blend chrominance (UV) while keeping luminance (Y) sharp
                uint r = (((curr >> 16) & 0xFF) * 3 + ((prev >> 16) & 0xFF)) / 4;
                uint g = (((curr >> 8) & 0xFF) * 3 + ((prev >> 8) & 0xFF)) / 4;
                uint b = ((curr & 0xFF) * 3 + (prev & 0xFF)) / 4;

                framebuffer[idx] = 0xFF000000 | (r << 16) | (g << 8) | b;
            }
        }
    }

    /// <summary>
    /// Convert framebuffer to a raw BMP byte array for screenshot saving.
    /// </summary>
    public static byte[] ToBmp(ReadOnlySpan<uint> framebuffer, int width, int height)
    {
        int rowSize = ((width * 3 + 3) / 4) * 4;
        int dataSize = rowSize * height;
        var bmp = new byte[54 + dataSize];

        // BMP header
        bmp[0] = (byte)'B'; bmp[1] = (byte)'M';
        BitConverter.GetBytes(54 + dataSize).CopyTo(bmp, 2);
        BitConverter.GetBytes(54).CopyTo(bmp, 10);
        BitConverter.GetBytes(40).CopyTo(bmp, 14);
        BitConverter.GetBytes(width).CopyTo(bmp, 18);
        BitConverter.GetBytes(height).CopyTo(bmp, 22);
        BitConverter.GetBytes((short)1).CopyTo(bmp, 26);
        BitConverter.GetBytes((short)24).CopyTo(bmp, 28);

        // Pixel data (BMP is bottom-up, BGR order)
        for (int y = 0; y < height; y++)
        {
            int srcRow = height - 1 - y;
            for (int x = 0; x < width; x++)
            {
                uint pixel = framebuffer[srcRow * width + x];
                int dstIdx = 54 + y * rowSize + x * 3;
                bmp[dstIdx] = (byte)(pixel & 0xFF);        // B
                bmp[dstIdx + 1] = (byte)((pixel >> 8) & 0xFF);  // G
                bmp[dstIdx + 2] = (byte)((pixel >> 16) & 0xFF); // R
            }
        }

        return bmp;
    }
}
