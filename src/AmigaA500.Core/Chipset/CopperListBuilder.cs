namespace AmigaA500.Core.Chipset;

/// <summary>
/// Fluent builder for constructing Copper lists programmatically.
/// Useful for tests and demo code.
/// </summary>
public sealed class CopperListBuilder
{
    private readonly List<ushort> _words = new();

    public CopperListBuilder Move(uint register, ushort value)
    {
        _words.Add((ushort)(register & 0x1FE));
        _words.Add(value);
        return this;
    }

    public CopperListBuilder Wait(int vpos, int hpos, int vmask = 0xFF, int hmask = 0xFE)
    {
        _words.Add((ushort)(((vpos & 0xFF) << 8) | (hpos & 0xFE) | 1));
        _words.Add((ushort)(((vmask & 0x7F) << 8) | (hmask & 0xFE)));
        return this;
    }

    public CopperListBuilder WaitLine(int line)
    {
        return Wait(line, 0x04, 0xFF, 0xFE);
    }

    public CopperListBuilder Skip(int vpos, int hpos)
    {
        _words.Add((ushort)(((vpos & 0xFF) << 8) | (hpos & 0xFE) | 1));
        _words.Add((ushort)(0xFFFE | 1)); // Same as WAIT but bit 0 of word 2 = 1
        return this;
    }

    public CopperListBuilder SetColor(int index, ushort rgb)
    {
        return Move((uint)(0x180 + index * 2), rgb);
    }

    public CopperListBuilder SetBitplanePointer(int plane, uint address)
    {
        uint regH = (uint)(0x0E0 + plane * 4);
        uint regL = regH + 2;
        Move(regH, (ushort)(address >> 16));
        Move(regL, (ushort)(address & 0xFFFF));
        return this;
    }

    public CopperListBuilder End()
    {
        _words.Add(0xFFFF);
        _words.Add(0xFFFE);
        return this;
    }

    public ushort[] Build() => _words.ToArray();

    public int SizeInBytes => _words.Count * 2;

    /// <summary>
    /// Write the copper list to memory via DMA write function.
    /// </summary>
    public void WriteTo(Action<uint, ushort> dmaWrite, uint address)
    {
        for (int i = 0; i < _words.Count; i++)
            dmaWrite(address + (uint)(i * 2), _words[i]);
    }
}
