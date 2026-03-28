using AmigaA500.Core.Chipset;

namespace AmigaA500.Tests.Chipset;

public class DmaSchedulerTests
{
    [Fact]
    public void CpuGetsSlots_WhenDmaDisabled()
    {
        var sched = new DmaScheduler();
        Assert.Equal(DmaOwner.Cpu, sched.GetSlotOwner(50, 0x0000));
    }

    [Fact]
    public void Refresh_AlwaysFirst4Slots()
    {
        var sched = new DmaScheduler();
        Assert.Equal(DmaOwner.Refresh, sched.GetSlotOwner(0, 0x03FF));
        Assert.Equal(DmaOwner.Refresh, sched.GetSlotOwner(3, 0x03FF));
    }

    [Fact]
    public void Disk_Slots4to6_WhenEnabled()
    {
        var sched = new DmaScheduler();
        Assert.Equal(DmaOwner.Disk, sched.GetSlotOwner(5, 0x0210)); // Master + Disk
    }

    [Fact]
    public void Audio_Slots7to10_WhenEnabled()
    {
        var sched = new DmaScheduler();
        Assert.Equal(DmaOwner.Audio, sched.GetSlotOwner(8, 0x0201)); // Master + AUD0
    }

    [Fact]
    public void AvailableCpuCycles_DecreasesWithDma()
    {
        var sched = new DmaScheduler();
        int noDma = sched.GetAvailableCpuCycles(0x0200, 0, false); // Master only
        int withBpl = sched.GetAvailableCpuCycles(0x0300, 5, false); // Master + 5 bitplanes

        Assert.True(noDma > withBpl);
    }
}
