namespace AmigaA500.Core.Diagnostics;

/// <summary>
/// Memory watchpoint system — triggers breakpoints on memory access.
/// </summary>
public sealed class MemoryWatcher
{
    private readonly Dictionary<uint, WatchType> _watchpoints = new();
    public event Action<uint, WatchType, uint>? OnHit;

    public void AddWatch(uint address, WatchType type)
    {
        _watchpoints[address] = type;
    }

    public void RemoveWatch(uint address)
    {
        _watchpoints.Remove(address);
    }

    public void CheckRead(uint address)
    {
        if (_watchpoints.TryGetValue(address, out var type) &&
            (type == WatchType.Read || type == WatchType.ReadWrite))
        {
            OnHit?.Invoke(address, WatchType.Read, 0);
        }
    }

    public void CheckWrite(uint address, uint value)
    {
        if (_watchpoints.TryGetValue(address, out var type) &&
            (type == WatchType.Write || type == WatchType.ReadWrite))
        {
            OnHit?.Invoke(address, WatchType.Write, value);
        }
    }

    public int Count => _watchpoints.Count;
    public void Clear() => _watchpoints.Clear();
}

public enum WatchType { Read, Write, ReadWrite }
