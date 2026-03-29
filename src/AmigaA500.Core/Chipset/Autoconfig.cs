namespace AmigaA500.Core.Chipset;

/// <summary>
/// Zorro II Autoconfig — automatic expansion board configuration.
/// Handles the autoconfig protocol at $E80000 for expansion memory and peripherals.
/// </summary>
public sealed class Autoconfig
{
    private readonly List<ExpansionBoard> _boards = new();
    private int _currentBoard;
    private bool _configDone;

    public void AddBoard(ExpansionBoard board)
    {
        _boards.Add(board);
    }

    public byte ReadByte(uint address)
    {
        if (_currentBoard >= _boards.Count || _configDone)
            return 0xFF;

        var board = _boards[_currentBoard];
        int reg = (int)((address - 0xE80000) >> 1) & 0x3F;

        return reg switch
        {
            0x00 => (byte)((board.Type << 4) | (board.SizeCode & 0x07)),
            0x01 => (byte)(board.ProductId),
            0x02 => (byte)((board.Flags << 4) | 0x0F),
            0x03 => 0xFF,
            0x04 => (byte)(board.ManufacturerId >> 8),
            0x05 => (byte)(board.ManufacturerId & 0xFF),
            0x06 => (byte)(board.SerialNumber >> 24),
            0x07 => (byte)(board.SerialNumber >> 16),
            0x08 => (byte)(board.SerialNumber >> 8),
            0x09 => (byte)(board.SerialNumber & 0xFF),
            _ => 0xFF
        };
    }

    public void WriteByte(uint address, byte value)
    {
        if (_currentBoard >= _boards.Count) return;

        int reg = (int)((address - 0xE80000) >> 1) & 0x3F;

        switch (reg)
        {
            case 0x24: // ec_BaseAddress (high nybble)
                var board = _boards[_currentBoard];
                board.BaseAddress = (uint)((value & 0xF0) << 16);
                board.Configured = true;
                _currentBoard++;
                break;
            case 0x26: // ec_Shutup
                _currentBoard++;
                break;
        }
    }

    public int BoardCount => _boards.Count;
    public int ConfiguredCount => _boards.Count(b => b.Configured);
}

public class ExpansionBoard
{
    public byte Type = 0xC0;        // Zorro II, memory, links into free list
    public byte SizeCode;            // 0=8MB, 1=64K, 2=128K, 3=256K, 4=512K, 5=1MB, 6=2MB, 7=4MB
    public byte ProductId;
    public byte Flags;
    public ushort ManufacturerId;
    public uint SerialNumber;
    public uint BaseAddress;
    public bool Configured;

    public int SizeInBytes => SizeCode switch
    {
        0 => 8 * 1024 * 1024,
        1 => 64 * 1024,
        2 => 128 * 1024,
        3 => 256 * 1024,
        4 => 512 * 1024,
        5 => 1024 * 1024,
        6 => 2 * 1024 * 1024,
        7 => 4 * 1024 * 1024,
        _ => 0
    };
}
