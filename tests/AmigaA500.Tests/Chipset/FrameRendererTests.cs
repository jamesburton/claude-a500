using AmigaA500.Core.Chipset;

namespace AmigaA500.Tests.Chipset;

public class FrameRendererTests
{
    [Fact]
    public void ClearScreen_FillsWithBackground()
    {
        var denise = new Denise();
        var sprites = new SpriteEngine();
        var fb = new uint[320 * 256];
        var renderer = new FrameRenderer(denise, sprites, fb);

        denise.SetColor(0, 0x0000); // Black background
        renderer.ClearScreen();

        Assert.Equal(0xFF000000u, fb[0]); // Opaque black
        Assert.Equal(0xFF000000u, fb[320 * 128 + 160]); // Center
    }

    [Fact]
    public void RenderLine_ProducesPixels()
    {
        var denise = new Denise();
        var sprites = new SpriteEngine();
        var fb = new uint[320 * 256];
        var renderer = new FrameRenderer(denise, sprites, fb);

        denise.BPLCON0 = 1 << 12; // 1 bitplane
        denise.SetColor(0, 0x0000); // Black
        denise.SetColor(1, 0x0FFF); // White

        var bpl0 = new ushort[20]; // 20 words × 16 bits = 320 pixels
        Array.Fill(bpl0, (ushort)0xFFFF); // All white

        renderer.RenderLine(0, bpl0, null, null, null, null, null);

        Assert.Equal(0xFFFFFFFFu, fb[0]); // White pixel
    }

    [Fact]
    public void RenderLine_IgnoresOutOfBounds()
    {
        var denise = new Denise();
        var sprites = new SpriteEngine();
        var fb = new uint[320 * 256];
        var renderer = new FrameRenderer(denise, sprites, fb);

        // Should not crash
        renderer.RenderLine(-1, null, null, null, null, null, null);
        renderer.RenderLine(256, null, null, null, null, null, null);
        renderer.RenderLine(999, null, null, null, null, null, null);
    }
}
