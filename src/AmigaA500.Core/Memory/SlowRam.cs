namespace AmigaA500.Core.Memory;

/// <summary>
/// Slow RAM (also known as Ranger RAM or C00000 RAM) — the 512 KB expansion memory
/// fitted to Amiga 500 trapdoor expansions. It lives in the $C00000–$C7FFFF range.
/// Slow RAM is accessible by the CPU and by Agnus DMA in chip-compatible mode, but
/// it is slower than Chip RAM because it shares the chip bus.
/// </summary>
public sealed class SlowRam
{
    /// <summary>Default size: 512 KB.</summary>
    public const int DefaultSize = 512 * 1024;

    /// <summary>Amiga base address for Slow RAM.</summary>
    public const uint BaseAddress = 0xC00000;

    /// <summary>Amiga end address (exclusive) for the default 512 KB region.</summary>
    public const uint EndAddress = 0xC80000;

    private readonly byte[] _data;

    /// <summary>
    /// Initialise Slow RAM.
    /// </summary>
    /// <param name="size">Size in bytes (default 512 KB). Must not exceed 512 KB.</param>
    public SlowRam(int size = DefaultSize)
    {
        if (size <= 0 || size > DefaultSize)
            throw new ArgumentOutOfRangeException(nameof(size),
                $"Slow RAM size must be between 1 and {DefaultSize} bytes.");
        _data = new byte[size];
    }

    /// <summary>Size of this Slow RAM region in bytes.</summary>
    public int Size => _data.Length;

    /// <summary>
    /// Returns true when <paramref name="address"/> falls within the Slow RAM
    /// address window ($C00000–$C00000+Size).
    /// </summary>
    public bool Contains(uint address) =>
        address >= BaseAddress && address < BaseAddress + (uint)_data.Length;

    // ------------------------------------------------------------------ CPU access

    /// <summary>Read a byte. Address must be within the Slow RAM window.</summary>
    public byte ReadByte(uint address)
    {
        uint offset = address - BaseAddress;
        if (offset >= (uint)_data.Length)
            throw new ArgumentOutOfRangeException(nameof(address),
                $"Slow RAM read at ${address:X6} out of range.");
        return _data[offset];
    }

    /// <summary>Write a byte. Address must be within the Slow RAM window.</summary>
    public void WriteByte(uint address, byte value)
    {
        uint offset = address - BaseAddress;
        if (offset >= (uint)_data.Length)
            throw new ArgumentOutOfRangeException(nameof(address),
                $"Slow RAM write at ${address:X6} out of range.");
        _data[offset] = value;
    }

    /// <summary>
    /// Read a big-endian 16-bit word. Address is forced to even alignment.
    /// </summary>
    public ushort ReadWord(uint address)
    {
        uint offset = (address & 0xFFFFFFFEu) - BaseAddress;
        if (offset + 1 >= (uint)_data.Length)
            throw new ArgumentOutOfRangeException(nameof(address),
                $"Slow RAM word read at ${address:X6} out of range.");
        return (ushort)(_data[offset] << 8 | _data[offset + 1]);
    }

    /// <summary>
    /// Write a big-endian 16-bit word. Address is forced to even alignment.
    /// </summary>
    public void WriteWord(uint address, ushort value)
    {
        uint offset = (address & 0xFFFFFFFEu) - BaseAddress;
        if (offset + 1 >= (uint)_data.Length)
            throw new ArgumentOutOfRangeException(nameof(address),
                $"Slow RAM word write at ${address:X6} out of range.");
        _data[offset] = (byte)(value >> 8);
        _data[offset + 1] = (byte)(value & 0xFF);
    }

    // ------------------------------------------------------------------ Bulk helpers

    /// <summary>
    /// Copy a block of bytes into Slow RAM from an external buffer.
    /// </summary>
    /// <param name="destAddress">Destination address in Slow RAM window.</param>
    /// <param name="source">Source bytes to copy.</param>
    public void BulkWrite(uint destAddress, ReadOnlySpan<byte> source)
    {
        uint offset = destAddress - BaseAddress;
        for (int i = 0; i < source.Length; i++)
        {
            uint idx = offset + (uint)i;
            if (idx >= (uint)_data.Length)
                throw new ArgumentOutOfRangeException(nameof(destAddress),
                    $"Slow RAM bulk write overflows at ${destAddress + i:X6}.");
            _data[idx] = source[i];
        }
    }

    /// <summary>
    /// Expose the underlying byte array as a span for direct access.
    /// </summary>
    public Span<byte> AsSpan() => _data.AsSpan();

    /// <summary>
    /// Clear all Slow RAM to zero.
    /// </summary>
    public void Clear() => Array.Clear(_data);
}
