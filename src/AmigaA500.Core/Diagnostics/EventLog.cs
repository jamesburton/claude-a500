namespace AmigaA500.Core.Diagnostics;

/// <summary>
/// Circular event log for emulator debugging. Records register writes, interrupt
/// requests, DMA activity, and custom events. The buffer wraps around so only the
/// most recent <see cref="Capacity"/> events are retained.
/// </summary>
public sealed class EventLog
{
    private readonly LogEvent[] _buffer;
    private int _head;   // Next write index
    private int _count;  // Total events written (unbounded)
    private readonly int _capacity;

    /// <summary>
    /// Initialise the event log with the given circular buffer capacity.
    /// </summary>
    /// <param name="capacity">Maximum number of events to retain (default 4096).</param>
    public EventLog(int capacity = 4096)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");
        _capacity = capacity;
        _buffer = new LogEvent[capacity];
    }

    /// <summary>Maximum number of retained events.</summary>
    public int Capacity => _capacity;

    /// <summary>Number of events currently in the buffer (≤ <see cref="Capacity"/>).</summary>
    public int Count => Math.Min(_count, _capacity);

    /// <summary>Total events ever recorded (monotonically increasing).</summary>
    public long TotalEvents => _count;

    // ------------------------------------------------------------------ Recording

    /// <summary>Record a custom chip register write.</summary>
    public void RegisterWrite(uint registerOffset, ushort value, long cycle = 0)
    {
        Append(new LogEvent
        {
            Type = LogEventType.RegisterWrite,
            Cycle = cycle,
            Address = registerOffset,
            Value = value
        });
    }

    /// <summary>Record an interrupt request (INTREQ bit set).</summary>
    public void Interrupt(int irqBit, long cycle = 0)
    {
        Append(new LogEvent
        {
            Type = LogEventType.Interrupt,
            Cycle = cycle,
            Address = (uint)irqBit,
            Value = 0
        });
    }

    /// <summary>Record a DMA channel event (start, end, or slot allocation).</summary>
    public void DmaEvent(int channel, DmaEventKind kind, uint address = 0, long cycle = 0)
    {
        Append(new LogEvent
        {
            Type = LogEventType.Dma,
            Cycle = cycle,
            Address = address,
            Value = (ushort)((channel & 0xFF) | ((int)kind << 8))
        });
    }

    /// <summary>Record a CPU memory access (read or write).</summary>
    public void CpuAccess(uint address, ushort value, bool write, long cycle = 0)
    {
        Append(new LogEvent
        {
            Type = write ? LogEventType.CpuWrite : LogEventType.CpuRead,
            Cycle = cycle,
            Address = address,
            Value = value
        });
    }

    /// <summary>Record an arbitrary labelled event.</summary>
    public void Custom(string label, uint data = 0, long cycle = 0)
    {
        Append(new LogEvent
        {
            Type = LogEventType.Custom,
            Cycle = cycle,
            Address = data,
            Value = 0,
            Label = label
        });
    }

    // ------------------------------------------------------------------ Retrieval

    /// <summary>
    /// Enumerate all retained events in chronological order (oldest first).
    /// </summary>
    public IEnumerable<LogEvent> GetEvents()
    {
        int stored = Count;
        if (stored == 0) yield break;

        // If buffer has wrapped, start from the oldest entry
        int start = _count > _capacity ? _head : 0;

        for (int i = 0; i < stored; i++)
        {
            int idx = (start + i) % _capacity;
            yield return _buffer[idx];
        }
    }

    /// <summary>
    /// Return the most recent <paramref name="n"/> events (or fewer if the log is smaller).
    /// </summary>
    public IReadOnlyList<LogEvent> GetRecent(int n)
    {
        var all = GetEvents().ToArray();
        int skip = Math.Max(0, all.Length - n);
        return all.Skip(skip).ToArray();
    }

    /// <summary>
    /// Return all events of the specified type.
    /// </summary>
    public IEnumerable<LogEvent> GetByType(LogEventType type) =>
        GetEvents().Where(e => e.Type == type);

    /// <summary>Clear the log.</summary>
    public void Clear()
    {
        _head = 0;
        _count = 0;
    }

    // ------------------------------------------------------------------ Private

    private void Append(LogEvent evt)
    {
        _buffer[_head] = evt;
        _head = (_head + 1) % _capacity;
        _count++;
    }
}

/// <summary>
/// A single entry in the <see cref="EventLog"/> circular buffer.
/// </summary>
public struct LogEvent
{
    /// <summary>Event classification.</summary>
    public LogEventType Type;

    /// <summary>Emulator clock cycle at which the event occurred.</summary>
    public long Cycle;

    /// <summary>
    /// Address or index associated with this event. Interpretation depends on <see cref="Type"/>.
    /// </summary>
    public uint Address;

    /// <summary>
    /// Value associated with this event (register value, DMA channel+kind, etc.).
    /// </summary>
    public ushort Value;

    /// <summary>Optional human-readable label (used by <see cref="LogEventType.Custom"/>).</summary>
    public string? Label;

    public override string ToString()
    {
        return Type switch
        {
            LogEventType.RegisterWrite => $"[{Cycle}] REG WRITE ${Address:X3} = ${Value:X4}",
            LogEventType.Interrupt     => $"[{Cycle}] IRQ bit {Address}",
            LogEventType.Dma           => $"[{Cycle}] DMA ch={Value & 0xFF} kind={(DmaEventKind)(Value >> 8)} addr=${Address:X6}",
            LogEventType.CpuRead       => $"[{Cycle}] CPU READ ${Address:X6} = ${Value:X4}",
            LogEventType.CpuWrite      => $"[{Cycle}] CPU WRITE ${Address:X6} = ${Value:X4}",
            LogEventType.Custom        => $"[{Cycle}] {Label} data=${Address:X8}",
            _                          => $"[{Cycle}] UNKNOWN"
        };
    }
}

/// <summary>Event classification for <see cref="LogEvent"/>.</summary>
public enum LogEventType : byte
{
    RegisterWrite,
    Interrupt,
    Dma,
    CpuRead,
    CpuWrite,
    Custom
}

/// <summary>DMA sub-event kinds recorded via <see cref="EventLog.DmaEvent"/>.</summary>
public enum DmaEventKind : byte
{
    Start,
    Complete,
    SlotAllocated,
    WordTransferred
}
