namespace AmigaA500.Core.Chipset;

/// <summary>
/// Disassembles Copper instruction lists into human-readable text for debugging.
/// Decodes MOVE, WAIT, and SKIP instructions with register names.
/// </summary>
public static class CopperDisassembler
{
    /// <summary>
    /// Disassemble a copper list from a byte array starting at the given offset.
    /// Stops at WAIT($1FE,$1FE) end-of-list sentinel or maxInstructions limit.
    /// </summary>
    public static IReadOnlyList<CopperInstruction> Disassemble(
        ReadOnlySpan<byte> memory,
        int offset = 0,
        int maxInstructions = 256)
    {
        var result = new List<CopperInstruction>();

        for (int i = 0; i < maxInstructions; i++)
        {
            int pos = offset + i * 4;
            if (pos + 3 >= memory.Length) break;

            ushort ir1 = (ushort)(memory[pos] << 8 | memory[pos + 1]);
            ushort ir2 = (ushort)(memory[pos + 2] << 8 | memory[pos + 3]);

            var instr = Decode(ir1, ir2, (uint)(pos));
            result.Add(instr);

            // WAIT($1FE,$1FE) is the conventional end-of-list marker
            if (instr.Type == CopperInstructionType.Wait &&
                instr.WaitVPos == 0xFF && instr.WaitHPos == 0xFE)
                break;
        }

        return result;
    }

    /// <summary>
    /// Disassemble a copper list from an array of 16-bit words.
    /// </summary>
    public static IReadOnlyList<CopperInstruction> DisassembleWords(
        ReadOnlySpan<ushort> words,
        uint baseAddress = 0,
        int maxInstructions = 256)
    {
        var result = new List<CopperInstruction>();

        for (int i = 0; i + 1 < words.Length && result.Count < maxInstructions; i += 2)
        {
            ushort ir1 = words[i];
            ushort ir2 = words[i + 1];
            uint address = baseAddress + (uint)(i * 2);

            var instr = Decode(ir1, ir2, address);
            result.Add(instr);

            if (instr.Type == CopperInstructionType.Wait &&
                instr.WaitVPos == 0xFF && instr.WaitHPos == 0xFE)
                break;
        }

        return result;
    }

    private static CopperInstruction Decode(ushort ir1, ushort ir2, uint address)
    {
        if ((ir1 & 1) == 0)
        {
            // MOVE instruction
            uint reg = (uint)(ir1 & 0x1FE);
            return new CopperInstruction
            {
                Address = address,
                IR1 = ir1,
                IR2 = ir2,
                Type = CopperInstructionType.Move,
                RegisterOffset = reg,
                RegisterName = GetRegisterName(reg),
                MoveValue = ir2,
                Text = $"MOVE #{ir2:X4}, {GetRegisterName(reg)}"
            };
        }
        else
        {
            // WAIT or SKIP
            bool isSkip = (ir2 & 1) != 0;
            int vp = (ir1 >> 8) & 0xFF;
            int hp = ir1 & 0xFE;
            int vm = (ir2 >> 8) & 0x7F;
            int hm = ir2 & 0xFE;
            bool bfd = (ir2 & 0x8000) != 0; // blitter-finished disable

            var type = isSkip ? CopperInstructionType.Skip : CopperInstructionType.Wait;
            string mnemonic = isSkip ? "SKIP" : "WAIT";
            string bfdStr = bfd ? "" : ",BFD";
            string text = $"{mnemonic} ({vp},{hp}), ({vm:X2},{hm:X2}){bfdStr}";

            return new CopperInstruction
            {
                Address = address,
                IR1 = ir1,
                IR2 = ir2,
                Type = type,
                WaitVPos = vp,
                WaitHPos = hp,
                WaitVMask = vm,
                WaitHMask = hm,
                BlitterFinishedDisable = bfd,
                Text = text
            };
        }
    }

    private static string GetRegisterName(uint offset) => offset switch
    {
        0x020 => "DMACONR",
        0x02A => "VPOSR",
        0x02C => "VHPOSR",
        0x040 => "BLTCON0",
        0x042 => "BLTCON1",
        0x044 => "BLTAFWM",
        0x046 => "BLTALWM",
        0x050 => "BLTAPT",
        0x060 => "BLTDPT",
        0x064 => "BLTSIZE",
        0x074 => "BLTAMOD",
        0x076 => "BLTBMOD",
        0x078 => "BLTCMOD",
        0x07A => "BLTDMOD",
        0x08A => "BPLCON0",
        0x08C => "BPLCON1",
        0x08E => "DIWSTRT",
        0x090 => "DIWSTOP",
        0x092 => "DDFSTRT",
        0x094 => "DDFSTOP",
        0x096 => "DMACON",
        0x098 => "CLXCON",
        0x09A => "INTENA",
        0x09C => "INTREQ",
        0x0A0 => "AUD0LCH",
        0x0A2 => "AUD0LCL",
        0x0A4 => "AUD0LEN",
        0x0A6 => "AUD0PER",
        0x0A8 => "AUD0VOL",
        0x0B0 => "AUD1LCH",
        0x0E0 => "BPL1PT",
        0x0E2 => "BPL1PTL",
        >= 0x180 and < 0x1C0 => $"COLOR{(offset - 0x180) / 2:D2}",
        >= 0x120 and < 0x180 => $"SPR{(offset - 0x120) / 8}PT",
        _ => $"${offset:X3}"
    };
}

/// <summary>
/// A decoded Copper instruction.
/// </summary>
public sealed class CopperInstruction
{
    /// <summary>Address of this instruction in chip RAM.</summary>
    public uint Address { get; init; }

    /// <summary>Raw first word of the instruction.</summary>
    public ushort IR1 { get; init; }

    /// <summary>Raw second word of the instruction.</summary>
    public ushort IR2 { get; init; }

    /// <summary>Instruction type: Move, Wait, or Skip.</summary>
    public CopperInstructionType Type { get; init; }

    // --- MOVE fields ---
    /// <summary>Register offset from $DFF000 (MOVE only).</summary>
    public uint RegisterOffset { get; init; }

    /// <summary>Register name string (MOVE only).</summary>
    public string RegisterName { get; init; } = string.Empty;

    /// <summary>Value to write (MOVE only).</summary>
    public ushort MoveValue { get; init; }

    // --- WAIT/SKIP fields ---
    /// <summary>Vertical beam position to wait for.</summary>
    public int WaitVPos { get; init; }

    /// <summary>Horizontal beam position to wait for.</summary>
    public int WaitHPos { get; init; }

    /// <summary>Vertical position compare mask.</summary>
    public int WaitVMask { get; init; }

    /// <summary>Horizontal position compare mask.</summary>
    public int WaitHMask { get; init; }

    /// <summary>When true, blitter-finished check is disabled for WAIT.</summary>
    public bool BlitterFinishedDisable { get; init; }

    /// <summary>Human-readable disassembly text.</summary>
    public string Text { get; init; } = string.Empty;

    public override string ToString() => $"${Address:X6}: {Text}";
}

/// <summary>
/// Copper instruction types.
/// </summary>
public enum CopperInstructionType
{
    /// <summary>Write a value to a custom chip register.</summary>
    Move,
    /// <summary>Pause execution until beam reaches a position.</summary>
    Wait,
    /// <summary>Skip the next instruction if beam has passed a position.</summary>
    Skip
}
