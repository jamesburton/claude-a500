namespace AmigaA500.Core.Cpu;

public sealed partial class Mc68000
{
    /// <summary>
    /// ABCD — Add Binary Coded Decimal with extend.
    /// </summary>
    internal void ExecuteAbcd(ushort opcode)
    {
        int srcReg = opcode & 7;
        int dstReg = (opcode >> 9) & 7;
        bool memMode = (opcode & 0x08) != 0;

        byte src, dst;
        uint srcAddr = 0, dstAddr = 0;

        if (memMode)
        {
            A[srcReg] -= 1;
            A[dstReg] -= 1;
            srcAddr = A[srcReg];
            dstAddr = A[dstReg];
            src = ReadByte(srcAddr);
            dst = ReadByte(dstAddr);
        }
        else
        {
            src = (byte)D[srcReg];
            dst = (byte)D[dstReg];
        }

        int lowNibble = (dst & 0xF) + (src & 0xF) + (X ? 1 : 0);
        if (lowNibble > 9) lowNibble += 6;

        int highNibble = (dst >> 4) + (src >> 4) + (lowNibble > 15 ? 1 : 0);
        if (highNibble > 9) highNibble += 6;

        byte result = (byte)(((highNibble & 0xF) << 4) | (lowNibble & 0xF));
        C = X = highNibble > 15;
        if (result != 0) Z = false; // Only clears Z, never sets it

        if (memMode)
            WriteByte(dstAddr, result);
        else
            D[dstReg] = (D[dstReg] & 0xFFFFFF00) | result;
    }

    /// <summary>
    /// SBCD — Subtract Binary Coded Decimal with extend.
    /// </summary>
    internal void ExecuteSbcd(ushort opcode)
    {
        int srcReg = opcode & 7;
        int dstReg = (opcode >> 9) & 7;
        bool memMode = (opcode & 0x08) != 0;

        byte src, dst;
        uint srcAddr = 0, dstAddr = 0;

        if (memMode)
        {
            A[srcReg] -= 1;
            A[dstReg] -= 1;
            srcAddr = A[srcReg];
            dstAddr = A[dstReg];
            src = ReadByte(srcAddr);
            dst = ReadByte(dstAddr);
        }
        else
        {
            src = (byte)D[srcReg];
            dst = (byte)D[dstReg];
        }

        int lowNibble = (dst & 0xF) - (src & 0xF) - (X ? 1 : 0);
        int borrow = 0;
        if (lowNibble < 0) { lowNibble += 10; borrow = 1; }

        int highNibble = (dst >> 4) - (src >> 4) - borrow;
        if (highNibble < 0) { highNibble += 10; C = X = true; }
        else { C = X = false; }

        byte result = (byte)(((highNibble & 0xF) << 4) | (lowNibble & 0xF));
        if (result != 0) Z = false;

        if (memMode)
            WriteByte(dstAddr, result);
        else
            D[dstReg] = (D[dstReg] & 0xFFFFFF00) | result;
    }

    /// <summary>
    /// NBCD — Negate Binary Coded Decimal.
    /// </summary>
    internal void ExecuteNbcd(ushort opcode)
    {
        int mode = (opcode >> 3) & 7;
        int reg = opcode & 7;

        byte val;
        if (mode == 0)
        {
            val = (byte)D[reg];
        }
        else
        {
            uint addr = GetEAAddress(mode, reg, 1);
            val = ReadByte(addr);
        }

        int lowNibble = -(val & 0xF) - (X ? 1 : 0);
        int borrow = 0;
        if (lowNibble < 0) { lowNibble += 10; borrow = 1; }

        int highNibble = -(val >> 4) - borrow;
        if (highNibble < 0) { highNibble += 10; C = X = true; }
        else { C = X = false; }

        byte result = (byte)(((highNibble & 0xF) << 4) | (lowNibble & 0xF));
        if (result != 0) Z = false;

        if (mode == 0)
            D[reg] = (D[reg] & 0xFFFFFF00) | result;
        else
        {
            uint addr = GetEAAddress(mode, reg, 1);
            WriteByte(addr, result);
        }
    }
}
