using AmigaA500.Core;

namespace AmigaA500.Tests;

/// <summary>
/// Simple flat memory bus for testing.
/// </summary>
public class TestBus : IBus
{
    private readonly byte[] _memory = new byte[1024 * 1024]; // 1 MB

    public byte ReadByte(uint address) => _memory[address & 0xFFFFF];
    public void WriteByte(uint address, byte value) => _memory[address & 0xFFFFF] = value;

    public ushort ReadWord(uint address)
    {
        address &= 0xFFFFF;
        return (ushort)(_memory[address] << 8 | _memory[address + 1]);
    }

    public void WriteWord(uint address, ushort value)
    {
        address &= 0xFFFFF;
        _memory[address] = (byte)(value >> 8);
        _memory[address + 1] = (byte)(value & 0xFF);
    }

    public void WriteWordAt(uint address, ushort value) => WriteWord(address, value);
    public void WriteLongAt(uint address, uint value)
    {
        WriteWord(address, (ushort)(value >> 16));
        WriteWord(address + 2, (ushort)(value & 0xFFFF));
    }

    public void LoadProgram(uint address, params ushort[] words)
    {
        for (int i = 0; i < words.Length; i++)
            WriteWord(address + (uint)(i * 2), words[i]);
    }
}
