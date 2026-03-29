namespace AmigaA500.Core.Cpu;

/// <summary>
/// Accurate 68000 instruction cycle timings.
/// Based on the Motorola M68000 Programmer's Reference Manual.
/// </summary>
public static class InstructionTimings
{
    /// <summary>
    /// Get the base cycle count for an effective address calculation.
    /// Does not include the bus access cycles (those are counted separately).
    /// </summary>
    public static int GetEACycles(int mode, int reg, int size)
    {
        return mode switch
        {
            0 => 0,             // Dn
            1 => 0,             // An
            2 => size == 4 ? 8 : 4,  // (An)
            3 => size == 4 ? 8 : 4,  // (An)+
            4 => size == 4 ? 10 : 6, // -(An)
            5 => size == 4 ? 12 : 8, // d16(An)
            6 => size == 4 ? 14 : 10, // d8(An,Xn)
            7 => reg switch
            {
                0 => size == 4 ? 12 : 8,  // abs.W
                1 => size == 4 ? 16 : 12, // abs.L
                2 => size == 4 ? 12 : 8,  // d16(PC)
                3 => size == 4 ? 14 : 10, // d8(PC,Xn)
                4 => size == 4 ? 12 : 8,  // #imm
                _ => 0
            },
            _ => 0
        };
    }

    /// <summary>
    /// Get base instruction cycles for common operations.
    /// </summary>
    public static int GetInstructionCycles(ushort opcode)
    {
        int group = opcode >> 12;
        return group switch
        {
            0x7 => 4,   // MOVEQ
            0x6 => 10,  // Bcc (taken), 8 (not taken), 18 (BSR)
            _ => 4      // Default minimum
        };
    }

    // MOVE timing: depends on source and destination EA
    public static int GetMoveCycles(int srcMode, int dstMode, int size)
    {
        int srcTime = GetEACycles(srcMode, 0, size);
        int dstTime = GetEACycles(dstMode, 0, size);
        return 4 + srcTime + dstTime; // Base + EA calculations
    }

    // MUL timing: 38 + 2n cycles where n = number of set bits
    public static int GetMulCycles(ushort operand, bool signed)
    {
        int setBits = 0;
        for (int i = 0; i < 16; i++)
            if ((operand & (1 << i)) != 0) setBits++;
        return 38 + setBits * 2;
    }

    // DIV timing: approximately 120-158 cycles
    public static int GetDivCycles(bool signed)
    {
        return signed ? 158 : 140; // Approximate worst case
    }

    // Shift/rotate: 6 + 2n for register, 8 for memory
    public static int GetShiftCycles(int count, bool isMemory)
    {
        return isMemory ? 8 : 6 + count * 2;
    }
}
