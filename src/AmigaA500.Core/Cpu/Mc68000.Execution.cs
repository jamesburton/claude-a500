namespace AmigaA500.Core.Cpu;

public sealed partial class Mc68000
{
    /// <summary>
    /// Execute a single instruction. Returns cycle count.
    /// </summary>
    public int ExecuteInstruction()
    {
        if (Halted) return 4;

        Cycles = 0;
        ushort opcode = FetchWord();

        int group = opcode >> 12;
        switch (group)
        {
            case 0x0: ExecuteGroup0(opcode); break;
            case 0x1: ExecuteMove(opcode, 1); break; // MOVE.B
            case 0x2: ExecuteMove(opcode, 4); break; // MOVE.L
            case 0x3: ExecuteMove(opcode, 2); break; // MOVE.W
            case 0x4: ExecuteGroup4(opcode); break;
            case 0x5: ExecuteGroup5(opcode); break;
            case 0x6: ExecuteBranch(opcode); break;
            case 0x7: ExecuteMoveQ(opcode); break;
            case 0x8: ExecuteGroup8(opcode); break;
            case 0x9: ExecuteGroup9(opcode); break;
            case 0xA: RaiseException(10); break; // Line-A
            case 0xB: ExecuteGroupB(opcode); break;
            case 0xC: ExecuteGroupC(opcode); break;
            case 0xD: ExecuteGroupD(opcode); break;
            case 0xE: ExecuteShift(opcode); break;
            case 0xF: RaiseException(11); break; // Line-F
        }

        TotalCycles += Cycles;
        return Cycles;
    }

    #region Addressing Modes

    private int GetSize(int sizeBits)
    {
        return sizeBits switch { 0 => 1, 1 => 2, 2 => 4, _ => 2 };
    }

    private uint ReadEA(int mode, int reg, int size)
    {
        return mode switch
        {
            0 => ReadDn(reg, size),
            1 => (uint)(size == 4 ? A[reg] : (ushort)A[reg]),
            2 => ReadMem(A[reg], size),
            3 => ReadMemPostInc(reg, size),
            4 => ReadMemPreDec(reg, size),
            5 => ReadMem((uint)(A[reg] + (short)FetchWord()), size),
            6 => ReadMemIndexed(A[reg]),
            7 => reg switch
            {
                0 => ReadMem((uint)(short)FetchWord(), size),
                1 => ReadMem(FetchLong(), size),
                2 => ReadMem((uint)(PC + (short)FetchWord() - 2), size),
                3 => ReadMemIndexed(PC - 2),
                4 => FetchImmediate(size),
                _ => 0
            },
            _ => 0
        };
    }

    private uint GetEAAddress(int mode, int reg, int size)
    {
        return mode switch
        {
            2 => A[reg],
            3 => PostIncrementAddress(reg, size),
            4 => PreDecrementAddress(reg, size),
            5 => (uint)(A[reg] + (short)FetchWord()),
            6 => ComputeIndexed(A[reg]),
            7 => reg switch
            {
                0 => (uint)(short)FetchWord(),
                1 => FetchLong(),
                2 => (uint)(PC + (short)FetchWord() - 2),
                3 => ComputeIndexed(PC - 2),
                _ => 0
            },
            _ => 0
        };
    }

    private void WriteEA(int mode, int reg, int size, uint value)
    {
        switch (mode)
        {
            case 0: WriteDn(reg, size, value); break;
            case 1: A[reg] = size == 2 ? (uint)(int)(short)value : value; break;
            default:
                uint addr = GetEAAddress(mode, reg, size);
                WriteMem(addr, size, value);
                break;
        }
    }

    private uint ReadDn(int reg, int size)
    {
        return size switch
        {
            1 => (byte)D[reg],
            2 => (ushort)D[reg],
            _ => D[reg]
        };
    }

    private void WriteDn(int reg, int size, uint value)
    {
        switch (size)
        {
            case 1: D[reg] = (D[reg] & 0xFFFFFF00) | (value & 0xFF); break;
            case 2: D[reg] = (D[reg] & 0xFFFF0000) | (value & 0xFFFF); break;
            default: D[reg] = value; break;
        }
    }

    private uint ReadMem(uint addr, int size)
    {
        return size switch
        {
            1 => ReadByte(addr),
            2 => ReadWord(addr),
            _ => ReadLong(addr)
        };
    }

    private void WriteMem(uint addr, int size, uint value)
    {
        switch (size)
        {
            case 1: WriteByte(addr, (byte)value); break;
            case 2: WriteWord(addr, (ushort)value); break;
            default: WriteLong(addr, value); break;
        }
    }

    private uint ReadMemPostInc(int reg, int size)
    {
        uint addr = A[reg];
        uint val = ReadMem(addr, size);
        A[reg] += (uint)(size == 1 && reg == 7 ? 2 : size); // SP always word-aligned
        return val;
    }

    private uint ReadMemPreDec(int reg, int size)
    {
        A[reg] -= (uint)(size == 1 && reg == 7 ? 2 : size);
        return ReadMem(A[reg], size);
    }

    private uint PostIncrementAddress(int reg, int size)
    {
        uint addr = A[reg];
        A[reg] += (uint)(size == 1 && reg == 7 ? 2 : size);
        return addr;
    }

    private uint PreDecrementAddress(int reg, int size)
    {
        A[reg] -= (uint)(size == 1 && reg == 7 ? 2 : size);
        return A[reg];
    }

    private uint ComputeIndexed(uint baseAddr)
    {
        ushort ext = FetchWord();
        int disp = (sbyte)(ext & 0xFF);
        int xReg = (ext >> 12) & 7;
        bool isAddr = (ext & 0x8000) != 0;
        bool isLong = (ext & 0x0800) != 0;
        int xVal = isAddr ? (int)A[xReg] : (int)D[xReg];
        if (!isLong) xVal = (short)xVal;
        return (uint)(baseAddr + disp + xVal);
    }

    private uint ReadMemIndexed(uint baseAddr)
    {
        uint addr = ComputeIndexed(baseAddr);
        // Size is determined by caller context — read word by default for indexed
        return ReadWord(addr);
    }

    private uint FetchImmediate(int size)
    {
        return size switch
        {
            1 => (byte)FetchWord(), // Byte immediate is in low byte of word
            2 => FetchWord(),
            _ => FetchLong()
        };
    }

    #endregion

    #region Flag Helpers

    private void SetFlagsNZ(uint result, int size)
    {
        uint mask = size switch { 1 => 0x80, 2 => 0x8000, _ => 0x80000000 };
        uint sizeMask = size switch { 1 => 0xFF, 2 => 0xFFFF, _ => 0xFFFFFFFF };
        N = (result & mask) != 0;
        Z = (result & sizeMask) == 0;
    }

    private void SetFlagsAdd(uint src, uint dst, uint result, int size)
    {
        uint mask = size switch { 1 => 0x80, 2 => 0x8000, _ => 0x80000000 };
        uint sizeMask = size switch { 1 => 0xFF, 2 => 0xFFFF, _ => 0xFFFFFFFF };
        result &= sizeMask;
        N = (result & mask) != 0;
        Z = result == 0;
        V = ((src ^ result) & (dst ^ result) & mask) != 0;
        C = ((src & dst) | ((src | dst) & ~result)) != 0 ? ((((src & dst) | ((src | dst) & ~result)) & mask) != 0) : false;
        X = C;
    }

    private void SetFlagsSub(uint src, uint dst, uint result, int size)
    {
        uint mask = size switch { 1 => 0x80, 2 => 0x8000, _ => 0x80000000 };
        uint sizeMask = size switch { 1 => 0xFF, 2 => 0xFFFF, _ => 0xFFFFFFFF };
        result &= sizeMask;
        N = (result & mask) != 0;
        Z = result == 0;
        V = ((dst ^ src) & (dst ^ result) & mask) != 0;
        C = ((src & ~dst) | (result & ~dst) | (src & result)) != 0 ? ((((src & ~dst) | (result & ~dst) | (src & result)) & mask) != 0) : false;
        X = C;
    }

    #endregion

    #region MOVE

    private void ExecuteMove(ushort opcode, int size)
    {
        int srcMode = (opcode >> 3) & 7;
        int srcReg = opcode & 7;
        int dstReg = (opcode >> 9) & 7;
        int dstMode = (opcode >> 6) & 7;

        uint value = ReadEA(srcMode, srcReg, size);

        if (dstMode == 1) // MOVEA
        {
            A[dstReg] = size == 2 ? (uint)(int)(short)value : value;
            return; // MOVEA doesn't affect flags
        }

        WriteEA(dstMode, dstReg, size, value);
        SetFlagsNZ(value, size);
        V = false;
        C = false;
    }

    #endregion

    #region MOVEQ

    private void ExecuteMoveQ(ushort opcode)
    {
        int reg = (opcode >> 9) & 7;
        int data = (sbyte)(opcode & 0xFF);
        D[reg] = (uint)data;
        SetFlagsNZ((uint)data, 4);
        V = false;
        C = false;
        Cycles += 0; // Only prefetch cycles counted in ReadWord
    }

    #endregion

    #region Group 0 — Immediate, Bit Operations

    private void ExecuteGroup0(ushort opcode)
    {
        if ((opcode & 0x0100) != 0)
        {
            // Dynamic bit operations (BTST/BCHG/BCLR/BSET with Dn)
            ExecuteBitDynamic(opcode);
            return;
        }

        int op = (opcode >> 9) & 7;
        switch (op)
        {
            case 0: ExecuteImmediate(opcode, ImmOp.Ori); break;
            case 1: ExecuteImmediate(opcode, ImmOp.Andi); break;
            case 2: ExecuteImmediate(opcode, ImmOp.Subi); break;
            case 3: ExecuteImmediate(opcode, ImmOp.Addi); break;
            case 4: ExecuteBitStatic(opcode); break;
            case 5: ExecuteImmediate(opcode, ImmOp.Eori); break;
            case 6: ExecuteImmediate(opcode, ImmOp.Cmpi); break;
        }
    }

    private enum ImmOp { Ori, Andi, Subi, Addi, Eori, Cmpi }

    private void ExecuteImmediate(ushort opcode, ImmOp op)
    {
        int size = GetSize((opcode >> 6) & 3);
        uint imm = FetchImmediate(size);
        int mode = (opcode >> 3) & 7;
        int reg = opcode & 7;

        // Special case: to SR/CCR
        if (mode == 7 && reg == 4)
        {
            if (op == ImmOp.Ori)
            {
                if (size == 1) CCR |= (byte)imm;
                else SR |= (ushort)imm;
            }
            else if (op == ImmOp.Andi)
            {
                if (size == 1) CCR &= (byte)imm;
                else SR &= (ushort)imm;
            }
            else if (op == ImmOp.Eori)
            {
                if (size == 1) CCR ^= (byte)imm;
                else SR ^= (ushort)imm;
            }
            return;
        }

        uint dst = ReadEA(mode, reg, size);
        uint result;

        switch (op)
        {
            case ImmOp.Ori:
                result = dst | imm;
                WriteEA(mode, reg, size, result);
                SetFlagsNZ(result, size); V = false; C = false;
                break;
            case ImmOp.Andi:
                result = dst & imm;
                WriteEA(mode, reg, size, result);
                SetFlagsNZ(result, size); V = false; C = false;
                break;
            case ImmOp.Subi:
                result = dst - imm;
                WriteEA(mode, reg, size, result);
                SetFlagsSub(imm, dst, result, size);
                break;
            case ImmOp.Addi:
                result = dst + imm;
                WriteEA(mode, reg, size, result);
                SetFlagsAdd(imm, dst, result, size);
                break;
            case ImmOp.Eori:
                result = dst ^ imm;
                WriteEA(mode, reg, size, result);
                SetFlagsNZ(result, size); V = false; C = false;
                break;
            case ImmOp.Cmpi:
                result = dst - imm;
                SetFlagsSub(imm, dst, result, size);
                break;
        }
    }

    private void ExecuteBitStatic(ushort opcode)
    {
        int bitOp = (opcode >> 6) & 3;
        int bitNum = (int)(FetchImmediate(2) & 0xFF);
        int mode = (opcode >> 3) & 7;
        int reg = opcode & 7;

        int size = mode == 0 ? 4 : 1;
        if (mode == 0) bitNum &= 31; else bitNum &= 7;

        uint val = ReadEA(mode, reg, size);
        Z = (val & (1u << bitNum)) == 0;

        switch (bitOp)
        {
            case 0: break; // BTST — just sets Z
            case 1: val ^= (1u << bitNum); WriteEA(mode, reg, size, val); break; // BCHG
            case 2: val &= ~(1u << bitNum); WriteEA(mode, reg, size, val); break; // BCLR
            case 3: val |= (1u << bitNum); WriteEA(mode, reg, size, val); break;  // BSET
        }
    }

    private void ExecuteBitDynamic(ushort opcode)
    {
        int bitOp = (opcode >> 6) & 3;
        int bitReg = (opcode >> 9) & 7;
        int bitNum = (int)D[bitReg];
        int mode = (opcode >> 3) & 7;
        int reg = opcode & 7;

        int size = mode == 0 ? 4 : 1;
        if (mode == 0) bitNum &= 31; else bitNum &= 7;

        uint val = ReadEA(mode, reg, size);
        Z = (val & (1u << bitNum)) == 0;

        switch (bitOp)
        {
            case 0: break;
            case 1: val ^= (1u << bitNum); WriteEA(mode, reg, size, val); break;
            case 2: val &= ~(1u << bitNum); WriteEA(mode, reg, size, val); break;
            case 3: val |= (1u << bitNum); WriteEA(mode, reg, size, val); break;
        }
    }

    #endregion

    #region Group 4 — Miscellaneous

    private void ExecuteGroup4(ushort opcode)
    {
        int subOp = (opcode >> 8) & 0xF;

        // LEA
        if ((opcode & 0x01C0) == 0x01C0 && (opcode & 0x0E00) != 0)
        {
            int reg = (opcode >> 9) & 7;
            int mode = (opcode >> 3) & 7;
            int srcReg = opcode & 7;
            A[reg] = GetEAAddress(mode, srcReg, 4);
            return;
        }

        // SWAP (must check before PEA — same base opcode, SWAP has mode=0)
        if ((opcode & 0xFFF8) == 0x4840 && ((opcode >> 3) & 7) == 0)
        {
            int reg = opcode & 7;
            D[reg] = (D[reg] >> 16) | (D[reg] << 16);
            SetFlagsNZ(D[reg], 4); V = false; C = false;
            return;
        }

        // PEA
        if ((opcode & 0xFFC0) == 0x4840)
        {
            int mode = (opcode >> 3) & 7;
            int reg = opcode & 7;
            uint addr = GetEAAddress(mode, reg, 4);
            A[7] -= 4;
            WriteLong(A[7], addr);
            return;
        }

        // CLR
        if ((opcode & 0xFF00) == 0x4200)
        {
            int size = GetSize((opcode >> 6) & 3);
            int mode = (opcode >> 3) & 7;
            int reg = opcode & 7;
            WriteEA(mode, reg, size, 0);
            N = false; Z = true; V = false; C = false;
            return;
        }

        // NEG
        if ((opcode & 0xFF00) == 0x4400)
        {
            int size = GetSize((opcode >> 6) & 3);
            int mode = (opcode >> 3) & 7;
            int reg = opcode & 7;
            uint val = ReadEA(mode, reg, size);
            uint result = 0 - val;
            WriteEA(mode, reg, size, result);
            SetFlagsSub(val, 0, result, size);
            return;
        }

        // NOT
        if ((opcode & 0xFF00) == 0x4600)
        {
            int size = GetSize((opcode >> 6) & 3);
            int mode = (opcode >> 3) & 7;
            int reg = opcode & 7;
            uint val = ReadEA(mode, reg, size);
            uint result = ~val;
            WriteEA(mode, reg, size, result);
            SetFlagsNZ(result, size); V = false; C = false;
            return;
        }

        // TST
        if ((opcode & 0xFF00) == 0x4A00)
        {
            int size = GetSize((opcode >> 6) & 3);
            int mode = (opcode >> 3) & 7;
            int reg = opcode & 7;
            uint val = ReadEA(mode, reg, size);
            SetFlagsNZ(val, size); V = false; C = false;
            return;
        }

        // EXT
        if ((opcode & 0xFEB8) == 0x4880)
        {
            int reg = opcode & 7;
            if ((opcode & 0x0040) != 0) // EXT.L
            {
                D[reg] = (uint)(int)(short)D[reg];
                SetFlagsNZ(D[reg], 4);
            }
            else // EXT.W
            {
                D[reg] = (D[reg] & 0xFFFF0000) | (uint)(ushort)(short)(sbyte)D[reg];
                SetFlagsNZ(D[reg], 2);
            }
            V = false; C = false;
            return;
        }

        // MOVEM
        if ((opcode & 0xFB80) == 0x4880)
        {
            ExecuteMovem(opcode);
            return;
        }

        // JSR
        if ((opcode & 0xFFC0) == 0x4E80)
        {
            int mode = (opcode >> 3) & 7;
            int reg = opcode & 7;
            uint addr = GetEAAddress(mode, reg, 4);
            A[7] -= 4;
            WriteLong(A[7], PC);
            PC = addr;
            return;
        }

        // JMP
        if ((opcode & 0xFFC0) == 0x4EC0)
        {
            int mode = (opcode >> 3) & 7;
            int reg = opcode & 7;
            PC = GetEAAddress(mode, reg, 4);
            return;
        }

        // RTS
        if (opcode == 0x4E75)
        {
            PC = ReadLong(A[7]);
            A[7] += 4;
            return;
        }

        // RTE
        if (opcode == 0x4E73)
        {
            SR = ReadWord(A[7]); A[7] += 2;
            PC = ReadLong(A[7]); A[7] += 4;
            return;
        }

        // NOP
        if (opcode == 0x4E71) return;

        // TRAP
        if ((opcode & 0xFFF0) == 0x4E40)
        {
            RaiseException(32 + (opcode & 0xF));
            return;
        }

        // LINK
        if ((opcode & 0xFFF8) == 0x4E50)
        {
            int reg = opcode & 7;
            short disp = (short)FetchWord();
            A[7] -= 4;
            WriteLong(A[7], A[reg]);
            A[reg] = A[7];
            A[7] = (uint)(A[7] + disp);
            return;
        }

        // UNLK
        if ((opcode & 0xFFF8) == 0x4E58)
        {
            int reg = opcode & 7;
            A[7] = A[reg];
            A[reg] = ReadLong(A[7]);
            A[7] += 4;
            return;
        }

        // MOVE USP
        if ((opcode & 0xFFF0) == 0x4E60)
        {
            int reg = opcode & 7;
            if ((opcode & 0x08) != 0)
                A[reg] = _usp; // USP → An
            else
                _usp = A[reg]; // An → USP
            return;
        }

        // STOP
        if (opcode == 0x4E72)
        {
            SR = FetchWord();
            Halted = true;
            return;
        }

        // RESET
        if (opcode == 0x4E70)
        {
            Cycles += 124; // Assert RESET line
            return;
        }

        // MOVE from SR
        if ((opcode & 0xFFC0) == 0x40C0)
        {
            int mode = (opcode >> 3) & 7;
            int reg = opcode & 7;
            WriteEA(mode, reg, 2, _sr);
            return;
        }

        // MOVE to SR
        if ((opcode & 0xFFC0) == 0x46C0)
        {
            int mode = (opcode >> 3) & 7;
            int reg = opcode & 7;
            SR = (ushort)ReadEA(mode, reg, 2);
            return;
        }

        // MOVE to CCR
        if ((opcode & 0xFFC0) == 0x44C0)
        {
            int mode = (opcode >> 3) & 7;
            int reg = opcode & 7;
            CCR = (byte)ReadEA(mode, reg, 2);
            return;
        }

        // NEGX
        if ((opcode & 0xFF00) == 0x4000)
        {
            int size = GetSize((opcode >> 6) & 3);
            int mode = (opcode >> 3) & 7;
            int reg = opcode & 7;
            uint val = ReadEA(mode, reg, size);
            uint result = 0 - val - (X ? 1u : 0u);
            WriteEA(mode, reg, size, result);
            SetFlagsSub(val, 0, result, size);
            uint sizeMask = size switch { 1 => 0xFF, 2 => 0xFFFF, _ => 0xFFFFFFFF };
            if ((result & sizeMask) != 0) Z = false; // NEGX only clears Z, never sets it
            return;
        }

        // Unhandled
        RaiseException(4); // Illegal instruction
    }

    private void ExecuteMovem(ushort opcode)
    {
        bool toLong = (opcode & 0x0040) != 0;
        int size = toLong ? 4 : 2;
        ushort mask = FetchWord();
        int mode = (opcode >> 3) & 7;
        int reg = opcode & 7;
        bool toMemory = (opcode & 0x0400) == 0;

        if (toMemory)
        {
            if (mode == 4) // Pre-decrement: registers stored in reverse
            {
                for (int i = 15; i >= 0; i--)
                {
                    if ((mask & (1 << (15 - i))) != 0)
                    {
                        A[reg] -= (uint)size;
                        uint val = i < 8 ? D[i] : A[i - 8];
                        if (size == 2) WriteWord(A[reg], (ushort)val);
                        else WriteLong(A[reg], val);
                    }
                }
            }
            else
            {
                uint addr = GetEAAddress(mode, reg, size);
                for (int i = 0; i < 16; i++)
                {
                    if ((mask & (1 << i)) != 0)
                    {
                        uint val = i < 8 ? D[i] : A[i - 8];
                        if (size == 2) WriteWord(addr, (ushort)val);
                        else WriteLong(addr, val);
                        addr += (uint)size;
                    }
                }
            }
        }
        else // To registers
        {
            uint addr;
            if (mode == 3) // Post-increment
                addr = A[reg];
            else
                addr = GetEAAddress(mode, reg, size);

            for (int i = 0; i < 16; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    uint val;
                    if (size == 2) val = (uint)(int)(short)ReadWord(addr);
                    else val = ReadLong(addr);
                    if (i < 8) D[i] = val; else A[i - 8] = val;
                    addr += (uint)size;
                }
            }
            if (mode == 3) A[reg] = addr;
        }
    }

    #endregion

    #region Group 5 — ADDQ/SUBQ/Scc/DBcc

    private void ExecuteGroup5(ushort opcode)
    {
        int size = GetSize((opcode >> 6) & 3);

        // Scc
        if ((opcode & 0x00C0) == 0x00C0)
        {
            int cond = (opcode >> 8) & 0xF;
            int mode = (opcode >> 3) & 7;
            int reg = opcode & 7;

            if (mode == 1) // DBcc
            {
                short disp = (short)FetchWord();
                if (!TestCondition(cond))
                {
                    int count = (short)D[reg] - 1;
                    D[reg] = (D[reg] & 0xFFFF0000) | (uint)(ushort)count;
                    if (count != -1)
                        PC = (uint)(PC - 2 + disp);
                }
                return;
            }

            WriteEA(mode, reg, 1, TestCondition(cond) ? 0xFFu : 0u);
            return;
        }

        // ADDQ / SUBQ
        int data = (opcode >> 9) & 7;
        if (data == 0) data = 8;
        int eaMode = (opcode >> 3) & 7;
        int eaReg = opcode & 7;

        if ((opcode & 0x0100) == 0) // ADDQ
        {
            if (eaMode == 1) // Address register: no flags affected
            {
                A[eaReg] += (uint)data;
                return;
            }
            uint val = ReadEA(eaMode, eaReg, size);
            uint result = val + (uint)data;
            WriteEA(eaMode, eaReg, size, result);
            SetFlagsAdd((uint)data, val, result, size);
        }
        else // SUBQ
        {
            if (eaMode == 1)
            {
                A[eaReg] -= (uint)data;
                return;
            }
            uint val = ReadEA(eaMode, eaReg, size);
            uint result = val - (uint)data;
            WriteEA(eaMode, eaReg, size, result);
            SetFlagsSub((uint)data, val, result, size);
        }
    }

    #endregion

    #region Group 6 — Bcc/BSR/BRA

    private void ExecuteBranch(ushort opcode)
    {
        int cond = (opcode >> 8) & 0xF;
        int disp = (sbyte)(opcode & 0xFF);
        uint target;

        if (disp == 0)
        {
            disp = (short)FetchWord();
            target = (uint)(PC - 2 + disp);
        }
        else
        {
            target = (uint)(PC + disp);
        }

        if (cond == 0) // BRA
        {
            PC = target;
            return;
        }

        if (cond == 1) // BSR
        {
            A[7] -= 4;
            WriteLong(A[7], PC);
            PC = target;
            return;
        }

        if (TestCondition(cond))
            PC = target;
    }

    #endregion

    #region Group 8 — OR/DIV/SBCD

    private void ExecuteGroup8(ushort opcode)
    {
        int reg = (opcode >> 9) & 7;
        int size = GetSize((opcode >> 6) & 3);
        int mode = (opcode >> 3) & 7;
        int eaReg = opcode & 7;

        // DIVU/DIVS
        if ((opcode & 0x01C0) == 0x00C0) // DIVU
        {
            uint divisor = ReadEA(mode, eaReg, 2) & 0xFFFF;
            if (divisor == 0) { RaiseException(5); return; }
            uint dividend = D[reg];
            uint quotient = dividend / divisor;
            uint remainder = dividend % divisor;
            if (quotient > 0xFFFF) { V = true; return; }
            D[reg] = (remainder << 16) | (quotient & 0xFFFF);
            SetFlagsNZ(quotient, 2); V = false; C = false;
            Cycles += 76; // Approximate
            return;
        }

        if ((opcode & 0x01C0) == 0x01C0) // DIVS
        {
            int divisor = (short)ReadEA(mode, eaReg, 2);
            if (divisor == 0) { RaiseException(5); return; }
            int dividend = (int)D[reg];
            int quotient = dividend / divisor;
            int remainder = dividend % divisor;
            if (quotient < -32768 || quotient > 32767) { V = true; return; }
            D[reg] = (uint)(((ushort)remainder << 16) | ((ushort)quotient));
            SetFlagsNZ((uint)(ushort)quotient, 2); V = false; C = false;
            Cycles += 120; // Approximate
            return;
        }

        // OR
        if ((opcode & 0x0100) != 0) // OR Dn,<ea>
        {
            uint dst = ReadEA(mode, eaReg, size);
            uint result = ReadDn(reg, size) | dst;
            WriteEA(mode, eaReg, size, result);
            SetFlagsNZ(result, size); V = false; C = false;
        }
        else // OR <ea>,Dn
        {
            uint src = ReadEA(mode, eaReg, size);
            uint result = ReadDn(reg, size) | src;
            WriteDn(reg, size, result);
            SetFlagsNZ(result, size); V = false; C = false;
        }
    }

    #endregion

    #region Group 9 — SUB/SUBX

    private void ExecuteGroup9(ushort opcode)
    {
        int reg = (opcode >> 9) & 7;
        int size = GetSize((opcode >> 6) & 3);
        int mode = (opcode >> 3) & 7;
        int eaReg = opcode & 7;

        // SUBA
        if ((opcode & 0x00C0) == 0x00C0 || (opcode & 0x01C0) == 0x01C0)
        {
            int srcSize = (opcode & 0x0100) != 0 ? 4 : 2;
            uint src = ReadEA(mode, eaReg, srcSize);
            if (srcSize == 2) src = (uint)(int)(short)src;
            A[reg] -= src;
            return;
        }

        // SUBX
        if ((opcode & 0x0130) == 0x0100 && mode <= 1)
        {
            // SUBX not fully implemented yet — placeholder
            uint src = ReadDn(eaReg, size);
            uint dst = ReadDn(reg, size);
            uint result = dst - src - (X ? 1u : 0u);
            WriteDn(reg, size, result);
            SetFlagsSub(src, dst, result, size);
            uint sizeMask = size switch { 1 => 0xFF, 2 => 0xFFFF, _ => 0xFFFFFFFF };
            if ((result & sizeMask) != 0) Z = false;
            return;
        }

        // SUB
        if ((opcode & 0x0100) != 0) // SUB Dn,<ea>
        {
            uint dst = ReadEA(mode, eaReg, size);
            uint src = ReadDn(reg, size);
            uint result = dst - src;
            WriteEA(mode, eaReg, size, result);
            SetFlagsSub(src, dst, result, size);
        }
        else // SUB <ea>,Dn
        {
            uint src = ReadEA(mode, eaReg, size);
            uint dst = ReadDn(reg, size);
            uint result = dst - src;
            WriteDn(reg, size, result);
            SetFlagsSub(src, dst, result, size);
        }
    }

    #endregion

    #region Group B — CMP/EOR

    private void ExecuteGroupB(ushort opcode)
    {
        int reg = (opcode >> 9) & 7;
        int size = GetSize((opcode >> 6) & 3);
        int mode = (opcode >> 3) & 7;
        int eaReg = opcode & 7;

        // CMPA
        if ((opcode & 0x00C0) == 0x00C0 || (opcode & 0x01C0) == 0x01C0)
        {
            int srcSize = (opcode & 0x0100) != 0 ? 4 : 2;
            uint src = ReadEA(mode, eaReg, srcSize);
            if (srcSize == 2) src = (uint)(int)(short)src;
            uint result = A[reg] - src;
            SetFlagsSub(src, A[reg], result, 4);
            X = !X; // CMP doesn't affect X — restore
            return;
        }

        // EOR Dn,<ea>
        if ((opcode & 0x0100) != 0)
        {
            uint dst = ReadEA(mode, eaReg, size);
            uint result = ReadDn(reg, size) ^ dst;
            WriteEA(mode, eaReg, size, result);
            SetFlagsNZ(result, size); V = false; C = false;
            return;
        }

        // CMP <ea>,Dn
        uint src2 = ReadEA(mode, eaReg, size);
        uint dst2 = ReadDn(reg, size);
        uint res = dst2 - src2;
        SetFlagsSub(src2, dst2, res, size);
        bool savedX = X;
        X = savedX; // CMP doesn't affect X
    }

    #endregion

    #region Group C — AND/MUL/ABCD/EXG

    private void ExecuteGroupC(ushort opcode)
    {
        int reg = (opcode >> 9) & 7;
        int size = GetSize((opcode >> 6) & 3);
        int mode = (opcode >> 3) & 7;
        int eaReg = opcode & 7;

        // MULU
        if ((opcode & 0x01C0) == 0x00C0)
        {
            uint src = ReadEA(mode, eaReg, 2) & 0xFFFF;
            uint result = (D[reg] & 0xFFFF) * src;
            D[reg] = result;
            SetFlagsNZ(result, 4); V = false; C = false;
            Cycles += 38; // Approximate
            return;
        }

        // MULS
        if ((opcode & 0x01C0) == 0x01C0)
        {
            int src = (short)ReadEA(mode, eaReg, 2);
            int result = (short)D[reg] * src;
            D[reg] = (uint)result;
            SetFlagsNZ((uint)result, 4); V = false; C = false;
            Cycles += 38;
            return;
        }

        // EXG
        if ((opcode & 0x0130) == 0x0100 && (mode == 0 || mode == 1))
        {
            int opMode = (opcode >> 3) & 0x1F;
            if (opMode == 0x08) // Dn,Dn
                (D[reg], D[eaReg]) = (D[eaReg], D[reg]);
            else if (opMode == 0x09) // An,An
                (A[reg], A[eaReg]) = (A[eaReg], A[reg]);
            else if (opMode == 0x11) // Dn,An
                (D[reg], A[eaReg]) = (A[eaReg], D[reg]);
            return;
        }

        // AND
        if ((opcode & 0x0100) != 0) // AND Dn,<ea>
        {
            uint dst = ReadEA(mode, eaReg, size);
            uint result = ReadDn(reg, size) & dst;
            WriteEA(mode, eaReg, size, result);
            SetFlagsNZ(result, size); V = false; C = false;
        }
        else // AND <ea>,Dn
        {
            uint src = ReadEA(mode, eaReg, size);
            uint result = ReadDn(reg, size) & src;
            WriteDn(reg, size, result);
            SetFlagsNZ(result, size); V = false; C = false;
        }
    }

    #endregion

    #region Group D — ADD/ADDX

    private void ExecuteGroupD(ushort opcode)
    {
        int reg = (opcode >> 9) & 7;
        int size = GetSize((opcode >> 6) & 3);
        int mode = (opcode >> 3) & 7;
        int eaReg = opcode & 7;

        // ADDA
        if ((opcode & 0x00C0) == 0x00C0 || (opcode & 0x01C0) == 0x01C0)
        {
            int srcSize = (opcode & 0x0100) != 0 ? 4 : 2;
            uint src = ReadEA(mode, eaReg, srcSize);
            if (srcSize == 2) src = (uint)(int)(short)src;
            A[reg] += src;
            return;
        }

        // ADDX
        if ((opcode & 0x0130) == 0x0100 && mode <= 1)
        {
            uint src = ReadDn(eaReg, size);
            uint dst = ReadDn(reg, size);
            uint result = dst + src + (X ? 1u : 0u);
            WriteDn(reg, size, result);
            SetFlagsAdd(src, dst, result, size);
            uint sizeMask = size switch { 1 => 0xFF, 2 => 0xFFFF, _ => 0xFFFFFFFF };
            if ((result & sizeMask) != 0) Z = false;
            return;
        }

        // ADD
        if ((opcode & 0x0100) != 0) // ADD Dn,<ea>
        {
            uint dst = ReadEA(mode, eaReg, size);
            uint src = ReadDn(reg, size);
            uint result = dst + src;
            WriteEA(mode, eaReg, size, result);
            SetFlagsAdd(src, dst, result, size);
        }
        else // ADD <ea>,Dn
        {
            uint src = ReadEA(mode, eaReg, size);
            uint dst = ReadDn(reg, size);
            uint result = dst + src;
            WriteDn(reg, size, result);
            SetFlagsAdd(src, dst, result, size);
        }
    }

    #endregion

    #region Group E — Shift/Rotate

    private void ExecuteShift(ushort opcode)
    {
        if ((opcode & 0x00C0) == 0x00C0) // Memory shift (word, shift by 1)
        {
            int type = (opcode >> 9) & 3;
            bool left = (opcode & 0x0100) != 0;
            int mode = (opcode >> 3) & 7;
            int reg = opcode & 7;
            uint val = ReadEA(mode, reg, 2);
            val = DoShift(type, left, val, 1, 2);
            WriteEA(mode, reg, 2, val);
            return;
        }

        // Register shift
        int shiftType = (opcode >> 3) & 3;
        bool shiftLeft = (opcode & 0x0100) != 0;
        int size = GetSize((opcode >> 6) & 3);
        int count;
        int destReg = opcode & 7;

        if ((opcode & 0x0020) != 0) // Count from register
        {
            count = (int)(D[(opcode >> 9) & 7] & 63);
        }
        else // Immediate count
        {
            count = (opcode >> 9) & 7;
            if (count == 0) count = 8;
        }

        uint value = ReadDn(destReg, size);
        value = DoShift(shiftType, shiftLeft, value, count, size);
        WriteDn(destReg, size, value);
        Cycles += count * 2;
    }

    private uint DoShift(int type, bool left, uint val, int count, int size)
    {
        uint mask = size switch { 1 => 0xFF, 2 => 0xFFFF, _ => 0xFFFFFFFF };
        uint msb = size switch { 1 => 0x80, 2 => 0x8000, _ => 0x80000000 };
        val &= mask;

        if (count == 0)
        {
            SetFlagsNZ(val, size); V = false; C = false;
            return val;
        }

        switch (type)
        {
            case 0: // ASR/ASL
                if (left)
                {
                    for (int i = 0; i < count; i++)
                    {
                        C = X = (val & msb) != 0;
                        val = (val << 1) & mask;
                    }
                }
                else
                {
                    bool sign = (val & msb) != 0;
                    for (int i = 0; i < count; i++)
                    {
                        C = X = (val & 1) != 0;
                        val = ((val >> 1) | (sign ? msb : 0)) & mask;
                    }
                }
                break;
            case 1: // LSR/LSL
                if (left)
                {
                    for (int i = 0; i < count; i++)
                    {
                        C = X = (val & msb) != 0;
                        val = (val << 1) & mask;
                    }
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        C = X = (val & 1) != 0;
                        val = (val >> 1) & mask;
                    }
                }
                break;
            case 2: // ROXR/ROXL
                if (left)
                {
                    for (int i = 0; i < count; i++)
                    {
                        bool oldX = X;
                        X = C = (val & msb) != 0;
                        val = ((val << 1) | (oldX ? 1u : 0u)) & mask;
                    }
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        bool oldX = X;
                        X = C = (val & 1) != 0;
                        val = ((val >> 1) | (oldX ? msb : 0)) & mask;
                    }
                }
                break;
            case 3: // ROR/ROL
                if (left)
                {
                    for (int i = 0; i < count; i++)
                    {
                        C = (val & msb) != 0;
                        val = ((val << 1) | (C ? 1u : 0u)) & mask;
                    }
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        C = (val & 1) != 0;
                        val = ((val >> 1) | (C ? msb : 0)) & mask;
                    }
                }
                break;
        }

        SetFlagsNZ(val, size);
        V = false; // Simplified — ASL should track overflow
        return val;
    }

    #endregion

    #region Condition Testing

    private bool TestCondition(int cond)
    {
        return cond switch
        {
            0 => true,                          // T
            1 => false,                         // F
            2 => !C && !Z,                      // HI
            3 => C || Z,                        // LS
            4 => !C,                            // CC (HS)
            5 => C,                             // CS (LO)
            6 => !Z,                            // NE
            7 => Z,                             // EQ
            8 => !V,                            // VC
            9 => V,                             // VS
            10 => !N,                           // PL
            11 => N,                            // MI
            12 => N == V,                       // GE
            13 => N != V,                       // LT
            14 => !Z && (N == V),               // GT
            15 => Z || (N != V),                // LE
            _ => true
        };
    }

    #endregion
}
