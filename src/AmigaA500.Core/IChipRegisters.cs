namespace AmigaA500.Core;

/// <summary>
/// Interface for custom chip register access ($DFF000-$DFF1FE).
/// </summary>
public interface IChipRegisters
{
    ushort ReadRegister(uint offset);
    void WriteRegister(uint offset, ushort value);
}
