namespace AmigaA500.Core.Diagnostics;

/// <summary>
/// Breakpoint manager for CPU debugging.
/// Supports PC breakpoints, register value breakpoints, and conditional breakpoints.
/// </summary>
public sealed class BreakpointManager
{
    private readonly HashSet<uint> _pcBreakpoints = new();
    private readonly List<ConditionalBreakpoint> _conditionals = new();

    public event Action<uint, string>? OnBreakpointHit;

    public void AddPcBreakpoint(uint address) => _pcBreakpoints.Add(address);
    public void RemovePcBreakpoint(uint address) => _pcBreakpoints.Remove(address);
    public bool HasPcBreakpoint(uint address) => _pcBreakpoints.Contains(address);

    public void AddConditional(string description, Func<bool> condition)
    {
        _conditionals.Add(new ConditionalBreakpoint { Description = description, Condition = condition });
    }

    /// <summary>
    /// Check all breakpoints. Returns true if any triggered.
    /// </summary>
    public bool Check(uint pc)
    {
        if (_pcBreakpoints.Contains(pc))
        {
            OnBreakpointHit?.Invoke(pc, $"PC breakpoint at ${pc:X6}");
            return true;
        }

        foreach (var bp in _conditionals)
        {
            if (bp.Enabled && bp.Condition())
            {
                OnBreakpointHit?.Invoke(pc, bp.Description);
                return true;
            }
        }

        return false;
    }

    public int Count => _pcBreakpoints.Count + _conditionals.Count;
    public void ClearAll() { _pcBreakpoints.Clear(); _conditionals.Clear(); }

    public IReadOnlySet<uint> PcBreakpoints => _pcBreakpoints;

    private class ConditionalBreakpoint
    {
        public string Description = "";
        public Func<bool> Condition = () => false;
        public bool Enabled = true;
    }
}
