namespace AmigaA500.Core.Cpu;

public sealed partial class Mc68000
{
    /// <summary>
    /// CHK — Check Register Against Bounds.
    /// If Dn < 0 or Dn > source, take CHK exception (vector 6).
    /// </summary>
    internal void ExecuteChk(ushort opcode)
    {
        int reg = (opcode >> 9) & 7;
        int mode = (opcode >> 3) & 7;
        int eaReg = opcode & 7;

        int bound = (short)ReadEA(mode, eaReg, 2);
        int value = (short)D[reg];

        if (value < 0)
        {
            N = true;
            RaiseException(6);
        }
        else if (value > bound)
        {
            N = false;
            RaiseException(6);
        }
    }

    /// <summary>
    /// TAS — Test and Set (read-modify-write with bus lock).
    /// Tests byte operand, sets condition flags, then sets bit 7.
    /// </summary>
    internal void ExecuteTas(ushort opcode)
    {
        int mode = (opcode >> 3) & 7;
        int reg = opcode & 7;

        uint val = ReadEA(mode, reg, 1);
        SetFlagsNZ(val, 1);
        V = false;
        C = false;

        // Set bit 7
        val |= 0x80;
        WriteEA(mode, reg, 1, val);
    }
}
