namespace AmigaA500.Core.Chipset;

/// <summary>
/// ECS (Enhanced Chip Set) extensions beyond the OCS baseline.
/// ECS Agnus supports 1MB chip RAM, ECS Denise adds productivity modes.
/// </summary>
public sealed class EcsExtensions
{
    // ECS Agnus features
    public bool EcsAgnus { get; set; }
    public int MaxChipRam => EcsAgnus ? 1024 * 1024 : 512 * 1024; // 1MB vs 512KB

    // ECS Denise features
    public bool EcsDenise { get; set; }

    // BPLCON3 — ECS Denise extended control
    public ushort BPLCON3;

    // BEAMCON0 — ECS beam counter control
    public ushort BEAMCON0;

    // Productivity modes (ECS Denise)
    public bool SuperHires => EcsDenise && (BPLCON3 & 0x0040) != 0; // 1280 pixels
    public bool ScanDoubled => EcsDenise && (BEAMCON0 & 0x0080) != 0;

    // FMODE — ECS/AGA fetch mode (wider DMA fetches)
    public ushort FMODE;
    public int FetchWidth => (FMODE & 3) switch
    {
        0 => 16,  // OCS: 16-bit fetches
        1 => 32,  // 32-bit fetches
        2 => 32,
        3 => 64,  // 64-bit fetches (AGA)
        _ => 16
    };

    // HTOTAL/VTOTAL — programmable beam counters (ECS)
    public ushort HTOTAL = 227;
    public ushort VTOTAL = 312;

    // Sprite improvements
    public bool WideSprites => EcsDenise; // ECS Denise allows wider sprites via attachment

    /// <summary>
    /// Check if a register address is an ECS-only register.
    /// Returns false for OCS registers.
    /// </summary>
    public bool IsEcsRegister(uint offset) => offset switch
    {
        0x106 => true, // BPLCON3
        0x1C0 => true, // HTOTAL
        0x1C2 => true, // HSSTOP
        0x1C4 => true, // HBSTRT
        0x1C6 => true, // HBSTOP
        0x1C8 => true, // VTOTAL
        0x1CA => true, // VSSTOP
        0x1CC => true, // VBSTRT
        0x1CE => true, // VBSTOP
        0x1DC => true, // BEAMCON0
        0x1DE => true, // HSSTRT
        0x1E0 => true, // VSSTRT
        0x1E2 => true, // HCENTER
        0x1E4 => true, // DIWHIGH
        0x1FC => true, // FMODE
        _ => false
    };

    public void Reset()
    {
        BPLCON3 = 0;
        BEAMCON0 = 0;
        FMODE = 0;
        HTOTAL = 227;
        VTOTAL = 312;
    }
}
