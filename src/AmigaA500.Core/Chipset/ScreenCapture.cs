namespace AmigaA500.Core.Chipset;

/// <summary>
/// Captures emulator framebuffer to image files for verification and debugging.
/// Supports BMP output (via VideoEncoder) and raw framebuffer dumps.
/// </summary>
public sealed class ScreenCapture
{
    private readonly uint[] _framebuffer;
    private readonly int _width;
    private readonly int _height;

    public ScreenCapture(uint[] framebuffer, int width = 320, int height = 256)
    {
        _framebuffer = framebuffer;
        _width = width;
        _height = height;
    }

    /// <summary>
    /// Save current framebuffer as a BMP file.
    /// </summary>
    public void SaveBmp(string path)
    {
        var bmp = VideoEncoder.ToBmp(_framebuffer, _width, _height);
        File.WriteAllBytes(path, bmp);
    }

    /// <summary>
    /// Save framebuffer as raw RGBA data (4 bytes per pixel).
    /// </summary>
    public void SaveRaw(string path)
    {
        var bytes = new byte[_framebuffer.Length * 4];
        Buffer.BlockCopy(_framebuffer, 0, bytes, 0, bytes.Length);
        File.WriteAllBytes(path, bytes);
    }

    /// <summary>
    /// Compare two framebuffers and return the number of differing pixels.
    /// </summary>
    public static int CompareFramebuffers(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        int diffs = 0;
        int len = Math.Min(a.Length, b.Length);
        for (int i = 0; i < len; i++)
            if (a[i] != b[i]) diffs++;
        return diffs;
    }

    /// <summary>
    /// Check if the framebuffer is entirely one color (blank screen).
    /// </summary>
    public bool IsBlank()
    {
        if (_framebuffer.Length == 0) return true;
        uint first = _framebuffer[0];
        for (int i = 1; i < Math.Min(_framebuffer.Length, _width * _height); i++)
            if (_framebuffer[i] != first) return false;
        return true;
    }

    /// <summary>
    /// Get a hash of the framebuffer for quick comparison.
    /// </summary>
    public uint GetHash()
    {
        uint hash = 0;
        for (int i = 0; i < Math.Min(_framebuffer.Length, _width * _height); i++)
            hash = hash * 31 + _framebuffer[i];
        return hash;
    }

    /// <summary>
    /// Extract a rectangular region from the framebuffer.
    /// </summary>
    public uint[] GetRegion(int x, int y, int w, int h)
    {
        var region = new uint[w * h];
        for (int row = 0; row < h; row++)
        {
            int srcIdx = (y + row) * _width + x;
            int dstIdx = row * w;
            if (srcIdx + w <= _framebuffer.Length)
                Array.Copy(_framebuffer, srcIdx, region, dstIdx, w);
        }
        return region;
    }
}
