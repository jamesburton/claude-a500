namespace AmigaA500.Core.Memory;

/// <summary>
/// Fast RAM (Zorro II) expansion at $200000-$9FFFFF.
/// Not accessible by DMA — CPU only. Faster than chip RAM for CPU operations.
/// </summary>
public sealed class FastRam
{
    private readonly byte[] _data;
    public const uint BaseAddress = 0x200000;

    public int Size => _data.Length;
    public uint EndAddress => BaseAddress + (uint)_data.Length;

    public FastRam(int sizeInBytes)
    {
        if (sizeInBytes < 0 || sizeInBytes > 8 * 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(sizeInBytes), "Fast RAM max 8MB");
        _data = new byte[sizeInBytes];
    }

    public bool Contains(uint address) =>
        address >= BaseAddress && address < EndAddress;

    public byte ReadByte(uint address) =>
        _data[address - BaseAddress];

    public void WriteByte(uint address, byte value) =>
        _data[address - BaseAddress] = value;

    public ushort ReadWord(uint address)
    {
        uint offset = address - BaseAddress;
        return (ushort)(_data[offset] << 8 | _data[offset + 1]);
    }

    public void WriteWord(uint address, ushort value)
    {
        uint offset = address - BaseAddress;
        _data[offset] = (byte)(value >> 8);
        _data[offset + 1] = (byte)(value & 0xFF);
    }

    public void Clear() => Array.Clear(_data);
}
