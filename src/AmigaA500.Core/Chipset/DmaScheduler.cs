namespace AmigaA500.Core.Chipset;

/// <summary>
/// DMA cycle scheduler — allocates bus cycles between DMA channels and CPU.
/// Each PAL scanline has 227 color clocks. DMA slots are allocated in priority order.
/// </summary>
public sealed class DmaScheduler
{
    // DMA slot allocation for a single scanline
    // Returns which device gets each color clock slot
    public DmaOwner GetSlotOwner(int hpos, ushort dmacon)
    {
        bool masterEn = (dmacon & 0x0200) != 0;
        if (!masterEn) return DmaOwner.Cpu;

        // Fixed allocations (highest priority first)
        if (hpos < 4) return DmaOwner.Refresh;
        if (hpos < 7 && (dmacon & 0x0010) != 0) return DmaOwner.Disk;
        if (hpos >= 7 && hpos < 11 && (dmacon & 0x000F) != 0) return DmaOwner.Audio;
        if (hpos >= 11 && hpos < 27 && (dmacon & 0x0020) != 0) return DmaOwner.Sprite;

        // Bitplane DMA: depends on mode and number of bitplanes
        if (hpos >= 0x38 && hpos <= 0xD8 && (dmacon & 0x0100) != 0)
            return DmaOwner.Bitplane;

        // Copper/Blitter share remaining slots
        if ((dmacon & 0x0080) != 0) return DmaOwner.Copper;
        if ((dmacon & 0x0040) != 0) return DmaOwner.Blitter;

        return DmaOwner.Cpu;
    }

    /// <summary>
    /// Calculate how many CPU cycles are available per scanline given current DMA configuration.
    /// </summary>
    public int GetAvailableCpuCycles(ushort dmacon, int numBitplanes, bool hires)
    {
        int totalSlots = 227;
        int dmaSlots = 4; // Refresh always

        if ((dmacon & 0x0010) != 0) dmaSlots += 3; // Disk
        if ((dmacon & 0x000F) != 0) dmaSlots += 4; // Audio (max)
        if ((dmacon & 0x0020) != 0) dmaSlots += 16; // Sprites

        // Bitplane DMA slots
        if ((dmacon & 0x0100) != 0)
        {
            int bplSlots = hires ? numBitplanes * 4 : numBitplanes * 2;
            dmaSlots += bplSlots * 10; // Approximate: depends on display width
        }

        return Math.Max(0, (totalSlots - dmaSlots) * 2); // 2 CPU cycles per color clock
    }
}

public enum DmaOwner
{
    Cpu,
    Refresh,
    Disk,
    Audio,
    Sprite,
    Bitplane,
    Copper,
    Blitter
}
