namespace AmigaA500.Core;

/// <summary>
/// Interface for CIA chip register access.
/// </summary>
public interface ICiaAccess
{
    byte ReadRegister(int index);
    void WriteRegister(int index, byte value);
}
