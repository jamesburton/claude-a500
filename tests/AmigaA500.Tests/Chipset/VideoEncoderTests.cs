using AmigaA500.Core.Chipset;

namespace AmigaA500.Tests.Chipset;

public class VideoEncoderTests
{
    [Fact]
    public void ToBmp_CorrectHeader()
    {
        var fb = new uint[4]; // 2x2
        var bmp = VideoEncoder.ToBmp(fb, 2, 2);
        Assert.Equal((byte)'B', bmp[0]);
        Assert.Equal((byte)'M', bmp[1]);
        Assert.True(bmp.Length > 54); // Has pixel data
    }

    [Fact]
    public void ToBmp_CorrectSize()
    {
        var fb = new uint[320 * 256];
        var bmp = VideoEncoder.ToBmp(fb, 320, 256);
        // 320 * 3 = 960 bytes per row (already aligned to 4)
        // 960 * 256 = 245760 + 54 header
        Assert.Equal(54 + 960 * 256, bmp.Length);
    }

    [Fact]
    public void ToBmp_RedPixel()
    {
        var fb = new uint[] { 0xFFFF0000 }; // Red
        var bmp = VideoEncoder.ToBmp(fb, 1, 1);
        // BMP is BGR: Blue=0, Green=0, Red=FF
        int rowSize = ((1 * 3 + 3) / 4) * 4; // 4 (padded)
        Assert.Equal(0x00, bmp[54]);     // B
        Assert.Equal(0x00, bmp[55]);     // G
        Assert.Equal(0xFF, bmp[56]);     // R
    }

    [Fact]
    public void Scale_2x()
    {
        var src = new uint[] { 0xFFFF0000, 0xFF00FF00, 0xFF0000FF, 0xFFFFFFFF }; // 2x2
        var dst = new uint[16]; // 4x4
        VideoEncoder.ScaleFramebuffer(src, dst, 2, 2, 4, 4);
        Assert.Equal(0xFFFF0000u, dst[0]); // Top-left = red
    }

    [Fact]
    public void Scale_WithScanlines()
    {
        var src = new uint[] { 0xFFFFFFFF }; // 1x1 white
        var dst = new uint[4]; // 2x2
        VideoEncoder.ScaleFramebuffer(src, dst, 1, 1, 2, 2, scanlines: true);
        Assert.Equal(0xFFFFFFFFu, dst[0]); // Row 0: full brightness
        // Row 1: scanline darkened
        uint scanlinePixel = dst[2];
        Assert.True(scanlinePixel != 0xFFFFFFFF); // Should be darker
    }

    [Fact]
    public void PalBlending_SmoothsColors()
    {
        var fb = new uint[4]; // 4x1
        fb[0] = 0xFF000000; // Black
        fb[1] = 0xFFFFFFFF; // White
        fb[2] = 0xFF000000; // Black
        fb[3] = 0xFFFFFFFF; // White

        VideoEncoder.ApplyPalBlending(fb, 4, 1);
        // fb[1] should be blended toward fb[0]
        Assert.NotEqual(0xFFFFFFFFu, fb[1]); // No longer pure white
    }
}
