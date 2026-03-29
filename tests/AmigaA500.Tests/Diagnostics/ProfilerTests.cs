using AmigaA500.Core.Diagnostics;

namespace AmigaA500.Tests.Diagnostics;

public class ProfilerTests
{
    [Fact]
    public void RecordInstruction_CountsCorrectly()
    {
        var profiler = new CpuProfiler();
        profiler.RecordInstruction(0x4E71); // NOP
        profiler.RecordInstruction(0x4E71); // NOP
        profiler.RecordInstruction(0x7000); // MOVEQ
        Assert.Equal(2, profiler.GetOpcodeCount(0x4E71));
        Assert.Equal(1, profiler.GetOpcodeCount(0x7000));
    }

    [Fact]
    public void GroupCount_TracksCorrectly()
    {
        var profiler = new CpuProfiler();
        profiler.RecordInstruction(0x4E71); // Group 4 (misc)
        profiler.RecordInstruction(0x4E75); // Group 4 (misc)
        profiler.RecordInstruction(0x7000); // Group 7 (MOVEQ)
        Assert.Equal(2, profiler.GetGroupCount(4));
        Assert.Equal(1, profiler.GetGroupCount(7));
    }

    [Fact]
    public void TopOpcodes_ReturnsMostFrequent()
    {
        var profiler = new CpuProfiler();
        for (int i = 0; i < 100; i++) profiler.RecordInstruction(0x4E71);
        for (int i = 0; i < 50; i++) profiler.RecordInstruction(0x7000);
        var top = profiler.GetTopOpcodes(2).ToList();
        Assert.Equal(0x4E71, top[0].opcode);
        Assert.Equal(100, top[0].count);
    }

    [Fact]
    public void TotalInstructions_Sums()
    {
        var profiler = new CpuProfiler();
        profiler.RecordInstruction(0x4E71);
        profiler.RecordInstruction(0x7000);
        profiler.RecordInstruction(0x6000);
        Assert.Equal(3, profiler.TotalInstructions);
    }

    [Fact]
    public void Reset_ClearsAll()
    {
        var profiler = new CpuProfiler();
        profiler.RecordInstruction(0x4E71);
        profiler.Reset();
        Assert.Equal(0, profiler.TotalInstructions);
    }
}

public class MemoryWatcherTests
{
    [Fact]
    public void WriteWatch_Triggers()
    {
        var watcher = new MemoryWatcher();
        bool hit = false;
        watcher.OnHit += (addr, type, val) => hit = true;
        watcher.AddWatch(0x1000, WatchType.Write);
        watcher.CheckWrite(0x1000, 42);
        Assert.True(hit);
    }

    [Fact]
    public void ReadWatch_DoesNotTriggerOnWrite()
    {
        var watcher = new MemoryWatcher();
        bool hit = false;
        watcher.OnHit += (addr, type, val) => hit = true;
        watcher.AddWatch(0x1000, WatchType.Read);
        watcher.CheckWrite(0x1000, 42);
        Assert.False(hit);
    }

    [Fact]
    public void ReadWriteWatch_TriggersBoth()
    {
        var watcher = new MemoryWatcher();
        int hitCount = 0;
        watcher.OnHit += (addr, type, val) => hitCount++;
        watcher.AddWatch(0x2000, WatchType.ReadWrite);
        watcher.CheckRead(0x2000);
        watcher.CheckWrite(0x2000, 0);
        Assert.Equal(2, hitCount);
    }

    [Fact]
    public void RemoveWatch_StopsTrigger()
    {
        var watcher = new MemoryWatcher();
        bool hit = false;
        watcher.OnHit += (addr, type, val) => hit = true;
        watcher.AddWatch(0x1000, WatchType.Write);
        watcher.RemoveWatch(0x1000);
        watcher.CheckWrite(0x1000, 42);
        Assert.False(hit);
    }
}
