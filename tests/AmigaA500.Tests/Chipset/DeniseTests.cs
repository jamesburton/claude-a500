using AmigaA500.Core.Chipset;

namespace AmigaA500.Tests.Chipset;

public class DeniseTests
{
    [Fact]
    public void Lores_5Bitplanes_32Colors()
    {
        var denise = new Denise();
        denise.BPLCON0 = 5 << 12; // 5 bitplanes, lores

        // Set all bitplanes to $FFFF (all bits set → index 31)
        for (int i = 0; i < 5; i++)
            denise.LoadBitplaneData(i, 0xFFFF);

        denise.SetColor(31, 0x0F0F); // Color 31 = purple
        Assert.Equal(0x0F0F, denise.GetPixelColor(0));
    }

    [Fact]
    public void Lores_SingleBitplane()
    {
        var denise = new Denise();
        denise.BPLCON0 = 1 << 12;

        denise.LoadBitplaneData(0, 0xAAAA); // Alternating bits
        denise.SetColor(0, 0x0000); // Background = black
        denise.SetColor(1, 0x0FFF); // Foreground = white

        Assert.Equal(0x0FFF, denise.GetPixelColor(0));  // Bit 15 = 1
        Assert.Equal(0x0000, denise.GetPixelColor(1));   // Bit 14 = 0
    }

    [Fact]
    public void HAM_SetFromPalette()
    {
        var denise = new Denise();
        denise.BPLCON0 = (6 << 12) | 0x0800; // 6 bitplanes + HAM
        denise.StartLine();

        denise.SetColor(5, 0x0ABC);
        // Control = 00 (palette), value = 5
        // Index = 5 → palette lookup
        for (int i = 0; i < 6; i++)
            denise.LoadBitplaneData(i, 0x0000);
        denise.LoadBitplaneData(0, 0xFFFF); // bit 0 = 1
        denise.LoadBitplaneData(2, 0xFFFF); // bit 2 = 1 → index = 5

        Assert.Equal(0x0ABC, denise.GetPixelColor(0));
    }

    [Fact]
    public void EHB_HalfBrightness()
    {
        var denise = new Denise();
        denise.BPLCON0 = 6 << 12; // 6 bitplanes, no HAM → EHB

        denise.SetColor(0, 0x0FFF); // White
        // Set all 6 bitplanes: index = 32 → EHB halves color 0
        denise.LoadBitplaneData(5, 0xFFFF); // Only bit 5 set → index 32
        for (int i = 0; i < 5; i++)
            denise.LoadBitplaneData(i, 0x0000);

        ushort color = denise.GetPixelColor(0);
        Assert.Equal(0x0777, color); // Half of $0FFF
    }

    [Fact]
    public void Rgb12ToRgba32_Red()
    {
        uint rgba = Denise.Rgb12ToRgba32(0x0F00);
        Assert.Equal(0xFFFF0000u, rgba); // Opaque red
    }

    [Fact]
    public void Rgb12ToRgba32_Green()
    {
        uint rgba = Denise.Rgb12ToRgba32(0x00F0);
        Assert.Equal(0xFF00FF00u, rgba);
    }

    [Fact]
    public void Rgb12ToRgba32_Blue()
    {
        uint rgba = Denise.Rgb12ToRgba32(0x000F);
        Assert.Equal(0xFF0000FFu, rgba);
    }

    [Fact]
    public void Rgb12ToRgba32_Black()
    {
        uint rgba = Denise.Rgb12ToRgba32(0x0000);
        Assert.Equal(0xFF000000u, rgba);
    }

    [Fact]
    public void DualPlayfield_PF1Priority()
    {
        var denise = new Denise();
        denise.BPLCON0 = (4 << 12) | 0x0400; // 4 bitplanes + dual playfield
        denise.BPLCON2 = 0x0000; // PF1 priority

        denise.SetColor(1, 0x0F00); // PF1 color 1 = red
        denise.SetColor(9, 0x00F0); // PF2 color 1 = green

        // PF1 has pixel (bitplane 0 set)
        denise.LoadBitplaneData(0, 0xFFFF);
        denise.LoadBitplaneData(1, 0xFFFF);  // PF2 also has pixel
        denise.LoadBitplaneData(2, 0x0000);
        denise.LoadBitplaneData(3, 0x0000);

        ushort color = denise.GetPixelColor(0);
        Assert.Equal(0x0F00, color); // PF1 wins (red)
    }
}
