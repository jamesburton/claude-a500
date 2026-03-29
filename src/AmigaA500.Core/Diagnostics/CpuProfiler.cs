namespace AmigaA500.Core.Diagnostics;

/// <summary>
/// CPU instruction profiler — tracks which opcodes are executed most frequently.
/// Useful for optimizing hot paths and verifying instruction coverage.
/// </summary>
public sealed class CpuProfiler
{
    private readonly long[] _opcodeCount = new long[65536];
    private readonly long[] _groupCount = new long[16];

    public void RecordInstruction(ushort opcode)
    {
        _opcodeCount[opcode]++;
        _groupCount[opcode >> 12]++;
    }

    public long GetOpcodeCount(ushort opcode) => _opcodeCount[opcode];
    public long GetGroupCount(int group) => _groupCount[group & 0xF];

    public IEnumerable<(ushort opcode, long count)> GetTopOpcodes(int n = 20)
    {
        return Enumerable.Range(0, 65536)
            .Where(i => _opcodeCount[i] > 0)
            .OrderByDescending(i => _opcodeCount[i])
            .Take(n)
            .Select(i => ((ushort)i, _opcodeCount[i]));
    }

    public string[] GetGroupSummary()
    {
        string[] names = { "Imm/Bit", "MOVE.B", "MOVE.L", "MOVE.W", "Misc", "ADDQ/Scc", "Bcc", "MOVEQ", "OR/DIV", "SUB", "Line-A", "CMP/EOR", "AND/MUL", "ADD", "Shift", "Line-F" };
        return names.Select((name, i) => $"{name}: {_groupCount[i]:N0}").ToArray();
    }

    public long TotalInstructions => _groupCount.Sum();

    public void Reset()
    {
        Array.Clear(_opcodeCount);
        Array.Clear(_groupCount);
    }
}
