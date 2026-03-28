namespace AmigaA500.Core.Chipset;

/// <summary>
/// Amiga Denise — video output chip. Converts bitplane data to pixel colors.
/// </summary>
public sealed class Denise
{
    private readonly ushort[] _color = new ushort[32];
    private readonly ushort[] _bplDat = new ushort[6];
    private readonly ushort[] _bplShift = new ushort[6];

    public ushort BPLCON0, BPLCON1, BPLCON2;

    public int NumBitplanes => (BPLCON0 >> 12) & 7;
    public bool IsHires => (BPLCON0 & 0x8000) != 0;
    public bool IsHAM => (BPLCON0 & 0x0800) != 0;
    public bool IsDualPlayfield => (BPLCON0 & 0x0400) != 0;
    public bool IsEHB => NumBitplanes == 6 && !IsHAM && !IsDualPlayfield;

    // Scroll delays
    public int PF1Delay => BPLCON1 & 0xF;
    public int PF2Delay => (BPLCON1 >> 4) & 0xF;

    // HAM state
    private ushort _hamPrevColor;

    public void SetColor(int index, ushort value)
    {
        if (index >= 0 && index < 32)
            _color[index] = (ushort)(value & 0x0FFF);
    }

    public ushort GetColor(int index) => index >= 0 && index < 32 ? _color[index] : (ushort)0;

    public void LoadBitplaneData(int plane, ushort data)
    {
        if (plane >= 0 && plane < 6)
        {
            _bplDat[plane] = data;
            _bplShift[plane] = data;
        }
    }

    /// <summary>
    /// Get the color index for pixel position within the current 16-pixel word.
    /// </summary>
    public int GetPixelIndex(int pixelInWord)
    {
        int index = 0;
        int numBpl = NumBitplanes;
        if (numBpl > 6) numBpl = 6;

        for (int i = 0; i < numBpl; i++)
        {
            int bit = (_bplShift[i] >> (15 - pixelInWord)) & 1;
            index |= bit << i;
        }
        return index;
    }

    /// <summary>
    /// Get the 12-bit RGB color for a pixel, handling all display modes.
    /// </summary>
    public ushort GetPixelColor(int pixelInWord)
    {
        int index = GetPixelIndex(pixelInWord);

        if (IsHAM)
            return DecodeHAM(index);

        if (IsEHB && index >= 32)
            return (ushort)((_color[index - 32] >> 1) & 0x0777);

        if (IsDualPlayfield)
            return DecodeDualPlayfield(pixelInWord);

        return _color[index & 0x1F];
    }

    /// <summary>
    /// Convert 12-bit Amiga RGB to 32-bit RGBA.
    /// </summary>
    public static uint Rgb12ToRgba32(ushort rgb12)
    {
        int r = (rgb12 >> 8) & 0xF;
        int g = (rgb12 >> 4) & 0xF;
        int b = rgb12 & 0xF;
        // Expand 4-bit to 8-bit: multiply by 17 (0x0 → 0x00, 0xF → 0xFF)
        return (uint)(0xFF000000 | (r * 17) << 16 | (g * 17) << 8 | (b * 17));
    }

    private ushort DecodeHAM(int data)
    {
        int control = (data >> 4) & 3;
        int value = data & 0xF;

        _hamPrevColor = control switch
        {
            0 => _color[value],
            1 => (ushort)((_hamPrevColor & 0xFF0) | value),        // Modify blue
            2 => (ushort)((_hamPrevColor & 0x0FF) | (value << 8)), // Modify red
            3 => (ushort)((_hamPrevColor & 0xF0F) | (value << 4)), // Modify green
            _ => _hamPrevColor
        };
        return _hamPrevColor;
    }

    private ushort DecodeDualPlayfield(int pixelInWord)
    {
        // Playfield 1: odd bitplanes (0, 2, 4) → colors 0-7
        int pf1Index = 0;
        if (NumBitplanes > 0) pf1Index |= ((_bplShift[0] >> (15 - pixelInWord)) & 1);
        if (NumBitplanes > 2) pf1Index |= ((_bplShift[2] >> (15 - pixelInWord)) & 1) << 1;
        if (NumBitplanes > 4) pf1Index |= ((_bplShift[4] >> (15 - pixelInWord)) & 1) << 2;

        // Playfield 2: even bitplanes (1, 3, 5) → colors 8-15
        int pf2Index = 0;
        if (NumBitplanes > 1) pf2Index |= ((_bplShift[1] >> (15 - pixelInWord)) & 1);
        if (NumBitplanes > 3) pf2Index |= ((_bplShift[3] >> (15 - pixelInWord)) & 1) << 1;
        if (NumBitplanes > 5) pf2Index |= ((_bplShift[5] >> (15 - pixelInWord)) & 1) << 2;

        // Priority: PF2P bit in BPLCON2
        bool pf2Priority = (BPLCON2 & 0x0040) != 0;

        if (pf2Priority)
        {
            if (pf2Index != 0) return _color[8 + pf2Index];
            if (pf1Index != 0) return _color[pf1Index];
        }
        else
        {
            if (pf1Index != 0) return _color[pf1Index];
            if (pf2Index != 0) return _color[8 + pf2Index];
        }

        return _color[0]; // Background
    }

    /// <summary>
    /// Reset HAM state at start of each line.
    /// </summary>
    public void StartLine()
    {
        _hamPrevColor = _color[0];
    }
}
