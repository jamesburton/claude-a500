namespace AmigaA500.Core;

/// <summary>
/// System bus interface for memory and I/O access.
/// </summary>
public interface IBus
{
    byte ReadByte(uint address);
    void WriteByte(uint address, byte value);
    ushort ReadWord(uint address);
    void WriteWord(uint address, ushort value);
}
