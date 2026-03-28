using AmigaA500.Core.Chipset;

namespace AmigaA500.Tests.Chipset;

public class SpriteTests
{
    [Fact]
    public void Sprite_ReturnsPixel_InRange()
    {
        var engine = new SpriteEngine();
        engine.SetPosition(0, 0x6440, 0x8000); // VStart=100, HStart=128
        engine.SetData(0, 0xFFFF, 0x0000); // Color 1 (all bits in DataA)

        int? pixel = engine.GetSpritePixel(128, 100);
        Assert.NotNull(pixel);
        Assert.Equal(17, pixel.Value); // Sprite 0 color 1 = COLOR17
    }

    [Fact]
    public void Sprite_ReturnsNull_OutOfRange()
    {
        var engine = new SpriteEngine();
        engine.SetPosition(0, 0x6440, 0x8000);
        engine.SetData(0, 0xFFFF, 0x0000);

        int? pixel = engine.GetSpritePixel(200, 100); // Wrong H position
        Assert.Null(pixel);
    }

    [Fact]
    public void Sprite_TransparentPixel()
    {
        var engine = new SpriteEngine();
        engine.SetPosition(0, 0x6440, 0x8000);
        engine.SetData(0, 0x0000, 0x0000); // All transparent

        int? pixel = engine.GetSpritePixel(128, 100);
        Assert.Null(pixel);
    }

    [Fact]
    public void Sprite_Priority()
    {
        var engine = new SpriteEngine();
        // Sprite 0 at same position as sprite 2
        engine.SetPosition(0, 0x6440, 0x8000);
        engine.SetData(0, 0xFFFF, 0x0000); // Color 1

        engine.SetPosition(2, 0x6440, 0x8000);
        engine.SetData(2, 0x0000, 0xFFFF); // Color 2

        int? pixel = engine.GetSpritePixel(128, 100);
        Assert.NotNull(pixel);
        Assert.Equal(17, pixel.Value); // Sprite 0 wins (higher priority)
    }
}
