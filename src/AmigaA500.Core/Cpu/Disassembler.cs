namespace AmigaA500.Core.Cpu;

/// <summary>
/// Simple 68000 disassembler for debugging.
/// </summary>
public static class Disassembler
{
    public static string Disassemble(IBus bus, uint address, out int length)
    {
        ushort opcode = bus.ReadWord(address);
        length = 2;

        int group = opcode >> 12;
        return group switch
        {
            0x0 => DisGroup0(bus, address, opcode, ref length),
            0x1 => $"MOVE.B {DisEA(bus, address + 2, (opcode >> 3) & 7, opcode & 7, 1, ref length)},{DisEADst(opcode)}",
            0x2 => $"MOVE.L {DisEA(bus, address + 2, (opcode >> 3) & 7, opcode & 7, 4, ref length)},{DisEADst(opcode)}",
            0x3 => $"MOVE.W {DisEA(bus, address + 2, (opcode >> 3) & 7, opcode & 7, 2, ref length)},{DisEADst(opcode)}",
            0x4 => DisGroup4(bus, address, opcode, ref length),
            0x5 => DisGroup5(opcode),
            0x6 => DisBranch(bus, address, opcode, ref length),
            0x7 => $"MOVEQ #{(sbyte)(opcode & 0xFF)},D{(opcode >> 9) & 7}",
            0x8 => $"OR/DIV ${opcode:X4}",
            0x9 => $"SUB ${opcode:X4}",
            0xA => $"LINE-A ${opcode:X4}",
            0xB => $"CMP/EOR ${opcode:X4}",
            0xC => $"AND/MUL ${opcode:X4}",
            0xD => $"ADD ${opcode:X4}",
            0xE => $"SHIFT ${opcode:X4}",
            0xF => $"LINE-F ${opcode:X4}",
            _ => $"??? ${opcode:X4}"
        };
    }

    private static string DisGroup0(IBus bus, uint addr, ushort op, ref int len)
    {
        if ((op & 0x0100) != 0) return $"BTST/BCHG/BCLR/BSET ${op:X4}";
        int imOp = (op >> 9) & 7;
        string name = imOp switch { 0 => "ORI", 1 => "ANDI", 2 => "SUBI", 3 => "ADDI", 5 => "EORI", 6 => "CMPI", _ => "???" };
        return $"{name} ${op:X4}";
    }

    private static string DisGroup4(IBus bus, uint addr, ushort op, ref int len)
    {
        if (op == 0x4E71) return "NOP";
        if (op == 0x4E75) return "RTS";
        if (op == 0x4E73) return "RTE";
        if (op == 0x4E70) return "RESET";
        if (op == 0x4E72) { len = 4; return $"STOP #${bus.ReadWord(addr + 2):X4}"; }
        if ((op & 0xFFF0) == 0x4E40) return $"TRAP #{op & 0xF}";
        if ((op & 0xFFF8) == 0x4E50) { len = 4; return $"LINK A{op & 7},#{(short)bus.ReadWord(addr + 2)}"; }
        if ((op & 0xFFF8) == 0x4E58) return $"UNLK A{op & 7}";
        if ((op & 0xFFC0) == 0x4E80) return $"JSR {DisEA(bus, addr + 2, (op >> 3) & 7, op & 7, 4, ref len)}";
        if ((op & 0xFFC0) == 0x4EC0) return $"JMP {DisEA(bus, addr + 2, (op >> 3) & 7, op & 7, 4, ref len)}";
        if ((op & 0xFF00) == 0x4200) return $"CLR.{SizeChar(op)} {DisEA(bus, addr + 2, (op >> 3) & 7, op & 7, GetSize(op), ref len)}";
        if ((op & 0xFF00) == 0x4A00) return $"TST.{SizeChar(op)} {DisEA(bus, addr + 2, (op >> 3) & 7, op & 7, GetSize(op), ref len)}";
        if ((op & 0x01C0) == 0x01C0) return $"LEA {DisEA(bus, addr + 2, (op >> 3) & 7, op & 7, 4, ref len)},A{(op >> 9) & 7}";
        return $"MISC ${op:X4}";
    }

    private static string DisGroup5(ushort op)
    {
        if ((op & 0x00C0) == 0x00C0)
        {
            int cond = (op >> 8) & 0xF;
            if (((op >> 3) & 7) == 1) return $"DB{CondName(cond)} D{op & 7},*";
            return $"S{CondName(cond)} D{op & 7}";
        }
        int data = (op >> 9) & 7; if (data == 0) data = 8;
        string name = (op & 0x0100) != 0 ? "SUBQ" : "ADDQ";
        return $"{name}.{SizeChar(op)} #{data},???";
    }

    private static string DisBranch(IBus bus, uint addr, ushort op, ref int len)
    {
        int cond = (op >> 8) & 0xF;
        int disp = (sbyte)(op & 0xFF);
        uint target;
        if (disp == 0)
        {
            disp = (short)bus.ReadWord(addr + 2);
            len = 4;
            target = (uint)(addr + 2 + disp);
        }
        else
        {
            target = (uint)(addr + 2 + disp);
        }
        string name = cond switch { 0 => "BRA", 1 => "BSR", _ => $"B{CondName(cond)}" };
        return $"{name} ${target:X6}";
    }

    private static string DisEA(IBus bus, uint extAddr, int mode, int reg, int size, ref int len)
    {
        switch (mode)
        {
            case 0: return $"D{reg}";
            case 1: return $"A{reg}";
            case 2: return $"(A{reg})";
            case 3: return $"(A{reg})+";
            case 4: return $"-(A{reg})";
            case 5: len += 2; return $"${(short)bus.ReadWord(extAddr):X}(A{reg})";
            case 6: len += 2; return $"d8(A{reg},Xi)";
            case 7:
                switch (reg)
                {
                    case 0: len += 2; return $"${bus.ReadWord(extAddr):X4}.W";
                    case 1: len += 4; return $"${(uint)(bus.ReadWord(extAddr) << 16 | bus.ReadWord(extAddr + 2)):X8}.L";
                    case 2: len += 2; return $"${(short)bus.ReadWord(extAddr):X}(PC)";
                    case 3: len += 2; return $"d8(PC,Xi)";
                    case 4:
                        if (size <= 2) { len += 2; return $"#${bus.ReadWord(extAddr):X4}"; }
                        else { len += 4; return $"#${(uint)(bus.ReadWord(extAddr) << 16 | bus.ReadWord(extAddr + 2)):X8}"; }
                }
                break;
        }
        return $"<ea:{mode}/{reg}>";
    }

    private static string DisEADst(ushort op)
    {
        int mode = (op >> 6) & 7;
        int reg = (op >> 9) & 7;
        return mode switch
        {
            0 => $"D{reg}", 1 => $"A{reg}", 2 => $"(A{reg})", 3 => $"(A{reg})+",
            4 => $"-(A{reg})", _ => $"<dst:{mode}/{reg}>"
        };
    }

    private static string CondName(int cond) => cond switch
    {
        0 => "T", 1 => "F", 2 => "HI", 3 => "LS", 4 => "CC", 5 => "CS",
        6 => "NE", 7 => "EQ", 8 => "VC", 9 => "VS", 10 => "PL", 11 => "MI",
        12 => "GE", 13 => "LT", 14 => "GT", 15 => "LE", _ => "??"
    };

    private static char SizeChar(ushort op) => ((op >> 6) & 3) switch { 0 => 'B', 1 => 'W', _ => 'L' };
    private static int GetSize(ushort op) => ((op >> 6) & 3) switch { 0 => 1, 1 => 2, _ => 4 };
}
