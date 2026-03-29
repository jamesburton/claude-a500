namespace AmigaA500.Core.Chipset;

/// <summary>
/// Gary gate array — the A500's address decoding and bus control chip.
/// Routes bus cycles between CPU, Agnus, CIAs, and expansion ports.
/// Also handles RESET timing and BERR (bus error) generation.
/// </summary>
public sealed class GaryGate
{
    // Bus owner tracking
    public BusOwner CurrentOwner { get; private set; } = BusOwner.Cpu;
    public bool CpuHalted { get; private set; }

    // OVL (overlay) state — directly from CIA-A PRA bit 0
    public bool Overlay { get; set; } = true;

    // RESET timing
    private int _resetCounter;
    public bool ResetActive => _resetCounter > 0;

    /// <summary>
    /// Decode an address to determine which device handles it.
    /// </summary>
    public BusTarget DecodeAddress(uint address)
    {
        address &= 0xFFFFFF; // 24-bit

        if (address < 0x200000)
        {
            if (address < 0x080000)
                return Overlay ? BusTarget.KickstartRom : BusTarget.ChipRam;
            if (address < 0x100000) return BusTarget.ChipRam; // ECS 1MB chip
            return BusTarget.None; // Reserved
        }

        if (address < 0xA00000) return BusTarget.ZorroII;     // Autoconfig expansion
        if (address < 0xBF0000) return BusTarget.None;         // Reserved
        if (address < 0xC00000) return BusTarget.Cia;          // CIA space
        if (address < 0xD00000) return BusTarget.SlowRam;      // Ranger/slow RAM
        if (address < 0xDC0000) return BusTarget.None;         // Reserved
        if (address < 0xDD0000) return BusTarget.RealTimeClock; // RTC
        if (address < 0xDF0000) return BusTarget.None;         // Reserved
        if (address < 0xE00000) return BusTarget.CustomChip;   // Custom registers
        if (address < 0xE80000) return BusTarget.None;         // Reserved
        if (address < 0xF00000) return BusTarget.Autoconfig;   // Autoconfig space
        if (address < 0xF80000) return BusTarget.CartridgeRom; // Cartridge/AR ROM
        return BusTarget.KickstartRom;                          // Kickstart ROM
    }

    /// <summary>
    /// Handle bus arbitration — give bus to DMA or CPU.
    /// </summary>
    public void ArbitrateBus(bool dmaRequest)
    {
        if (dmaRequest)
        {
            CurrentOwner = BusOwner.Dma;
            CpuHalted = true;
        }
        else
        {
            CurrentOwner = BusOwner.Cpu;
            CpuHalted = false;
        }
    }

    /// <summary>
    /// Process RESET instruction — asserts RESET line for 124 cycles.
    /// </summary>
    public void AssertReset()
    {
        _resetCounter = 124;
    }

    public void Tick()
    {
        if (_resetCounter > 0) _resetCounter--;
    }
}

public enum BusOwner { Cpu, Dma }

public enum BusTarget
{
    ChipRam, SlowRam, KickstartRom, CartridgeRom,
    CustomChip, Cia, RealTimeClock, Autoconfig, ZorroII, None
}
