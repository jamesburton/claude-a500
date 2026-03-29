namespace AmigaA500.Core.Memory;

/// <summary>
/// Dedicated Chip RAM implementation with bounds checking and a DMA port.
/// The Amiga 500 ships with 512 KB of chip RAM, expandable to 1 MB with a trapdoor
/// expansion. All custom chips (Agnus, Denise, Paula, Copper, Blitter) share the
/// DMA port, which bypasses the CPU address decoder.
/// </summary>
public sealed class ChipRam
{
    private readonly byte[] _data;
    private readonly uint _mask;

    /// <summary>
    /// Initialise Chip RAM.
    /// </summary>
    /// <param name="size">Size in bytes — must be a power of two (default 512 KB).</param>
    public ChipRam(int size = 512 * 1024)
    {
        if (size <= 0 || (size & (size - 1)) != 0)
            throw new ArgumentException("ChipRam size must be a positive power of two.", nameof(size));

        _data = new byte[size];
        _mask = (uint)(size - 1);
    }

    /// <summary>Size of this Chip RAM region in bytes.</summary>
    public int Size => _data.Length;

    // ------------------------------------------------------------------ CPU access

    /// <summary>
    /// Read a byte from Chip RAM. Throws <see cref="ArgumentOutOfRangeException"/>
    /// if <paramref name="address"/> is out of range.
    /// </summary>
    public byte ReadByte(uint address)
    {
        if (address >= (uint)_data.Length)
            throw new ArgumentOutOfRangeException(nameof(address),
                $"Chip RAM read at ${address:X6} out of range (size=${_data.Length:X}).");
        return _data[address];
    }

    /// <summary>
    /// Write a byte to Chip RAM. Throws <see cref="ArgumentOutOfRangeException"/>
    /// if <paramref name="address"/> is out of range.
    /// </summary>
    public void WriteByte(uint address, byte value)
    {
        if (address >= (uint)_data.Length)
            throw new ArgumentOutOfRangeException(nameof(address),
                $"Chip RAM write at ${address:X6} out of range (size=${_data.Length:X}).");
        _data[address] = value;
    }

    /// <summary>
    /// Read a big-endian 16-bit word from Chip RAM. Address is forced to even alignment.
    /// Throws <see cref="ArgumentOutOfRangeException"/> on out-of-range access.
    /// </summary>
    public ushort ReadWord(uint address)
    {
        address &= 0xFFFFFFFEu;
        if (address + 1 >= (uint)_data.Length)
            throw new ArgumentOutOfRangeException(nameof(address),
                $"Chip RAM word read at ${address:X6} out of range (size=${_data.Length:X}).");
        return (ushort)(_data[address] << 8 | _data[address + 1]);
    }

    /// <summary>
    /// Write a big-endian 16-bit word to Chip RAM. Address is forced to even alignment.
    /// Throws <see cref="ArgumentOutOfRangeException"/> on out-of-range access.
    /// </summary>
    public void WriteWord(uint address, ushort value)
    {
        address &= 0xFFFFFFFEu;
        if (address + 1 >= (uint)_data.Length)
            throw new ArgumentOutOfRangeException(nameof(address),
                $"Chip RAM word write at ${address:X6} out of range (size=${_data.Length:X}).");
        _data[address] = (byte)(value >> 8);
        _data[address + 1] = (byte)(value & 0xFF);
    }

    // ------------------------------------------------------------------ DMA port

    /// <summary>
    /// DMA read: reads a 16-bit word, wrapping the address within Chip RAM.
    /// No bounds exception — DMA addresses are always masked.
    /// </summary>
    public ushort DmaReadWord(uint address)
    {
        address &= _mask & 0xFFFFFFFEu;
        return (ushort)(_data[address] << 8 | _data[address + 1]);
    }

    /// <summary>
    /// DMA write: writes a 16-bit word, wrapping the address within Chip RAM.
    /// No bounds exception — DMA addresses are always masked.
    /// </summary>
    public void DmaWriteWord(uint address, ushort value)
    {
        address &= _mask & 0xFFFFFFFEu;
        _data[address] = (byte)(value >> 8);
        _data[address + 1] = (byte)(value & 0xFF);
    }

    /// <summary>
    /// DMA read byte with address masking.
    /// </summary>
    public byte DmaReadByte(uint address)
    {
        address &= _mask;
        return _data[address];
    }

    // ------------------------------------------------------------------ Bulk helpers

    /// <summary>
    /// Copy a block of bytes into Chip RAM from an external buffer.
    /// Useful for loading ROM data or disk sectors via DMA.
    /// </summary>
    /// <param name="destAddress">Destination address in Chip RAM (masked).</param>
    /// <param name="source">Source bytes to copy.</param>
    public void BulkWrite(uint destAddress, ReadOnlySpan<byte> source)
    {
        for (int i = 0; i < source.Length; i++)
            _data[(destAddress + i) & _mask] = source[i];
    }

    /// <summary>
    /// Expose the underlying byte array as a span for direct access (e.g. rendering).
    /// </summary>
    public Span<byte> AsSpan() => _data.AsSpan();

    /// <summary>
    /// Clear all Chip RAM to zero.
    /// </summary>
    public void Clear() => Array.Clear(_data);
}
