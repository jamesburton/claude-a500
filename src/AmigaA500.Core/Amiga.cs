using AmigaA500.Core.Chipset;
using AmigaA500.Core.Cia;
using AmigaA500.Core.Cpu;
using AmigaA500.Core.Floppy;
using AmigaA500.Core.Memory;

namespace AmigaA500.Core;

/// <summary>
/// Amiga 500 system — integrates CPU, chipset, CIA, memory, and floppy.
/// </summary>
public sealed class Amiga
{
    public Mc68000 Cpu { get; }
    public AddressBus Bus { get; }
    public CustomRegisters Custom { get; }
    public Cia8520 CiaA { get; }
    public Cia8520 CiaB { get; }
    public FloppyDrive[] Drives { get; } = new FloppyDrive[4];

    // Timing
    public const int CpuClocksPerLine = 455;   // PAL: 227.5 color clocks × 2
    public const int LinesPerFrame = 312;       // PAL
    public const double CpuClockHz = 7_093_790; // PAL

    private int _hPos;
    private int _vPos;
    private int _pendingInterruptLevel;

    // Framebuffer for video output (320×256 × 32-bit RGBA)
    public readonly uint[] Framebuffer = new uint[320 * 256];
    public bool FrameReady { get; private set; }

    public Amiga(byte[] kickstartRom)
    {
        Custom = new CustomRegisters();
        CiaA = new Cia8520();
        CiaB = new Cia8520();

        Bus = new AddressBus(512 * 1024, kickstartRom, Custom, CiaA, CiaB);
        Cpu = new Mc68000(Bus);

        for (int i = 0; i < 4; i++)
            Drives[i] = new FloppyDrive();

        // Wire up interrupts
        Custom.OnInterruptRequest = level => _pendingInterruptLevel = level;
        CiaA.OnInterrupt = _ => Custom.RequestInterrupt(3); // PORTS (level 2)
        CiaB.OnInterrupt = _ => Custom.RequestInterrupt(13); // EXTER (level 6)

        // Wire up beam position reads
        Custom.ReadVPOSR = () => (ushort)((_vPos >> 8) & 1);
        Custom.ReadVHPOSR = () => (ushort)(((_vPos & 0xFF) << 8) | (_hPos >> 1));

        // CIA-A port reads
        CiaA.ReadPortAExternal = () =>
        {
            byte val = 0xFF;
            // Bit 5: /RDY
            if (GetSelectedDrive()?.Ready != true) val &= 0xDF;
            // Bit 4: /TK0
            if (GetSelectedDrive()?.AtTrackZero != true) val &= 0xEF;
            // Bit 3: /WPRO
            if (GetSelectedDrive()?.WriteProtected == true) val &= 0xF7;
            // Bit 2: /CHNG
            if (GetSelectedDrive()?.DiskInserted != true) val &= 0xFB;
            return val;
        };

        // CIA-B port B: disk drive control
        CiaB.ReadPortBExternal = () => 0xFF;
    }

    public void Reset()
    {
        Bus.Overlay = true;
        Cpu.Reset();
        _hPos = 0;
        _vPos = 0;
        FrameReady = false;
    }

    /// <summary>
    /// Execute one CPU instruction and advance timing.
    /// </summary>
    public int Step()
    {
        // Check pending interrupts
        if (_pendingInterruptLevel > 0)
        {
            Cpu.RaiseInterrupt(_pendingInterruptLevel);
            _pendingInterruptLevel = 0;
        }

        int cycles = Cpu.ExecuteInstruction();

        // Advance beam position
        for (int i = 0; i < cycles; i++)
        {
            AdvanceBeam();
        }

        // Handle OVL bit from CIA-A
        bool ovl = (CiaA.PRA & CiaA.DDRA & 0x01) != 0;
        if (!ovl && Bus.Overlay)
            Bus.Overlay = false;

        return cycles;
    }

    /// <summary>
    /// Run for one complete frame (312 lines × 455 clocks).
    /// </summary>
    public void RunFrame()
    {
        FrameReady = false;
        long targetCycles = Cpu.TotalCycles + (CpuClocksPerLine * LinesPerFrame);
        while (Cpu.TotalCycles < targetCycles)
        {
            Step();
        }
        FrameReady = true;
    }

    private void AdvanceBeam()
    {
        _hPos++;
        if (_hPos >= CpuClocksPerLine)
        {
            _hPos = 0;
            _vPos++;

            if (_vPos >= LinesPerFrame)
            {
                _vPos = 0;
                // VBLANK interrupt
                Custom.RequestInterrupt(5); // VERTB
                FrameReady = true;
            }
        }

        // CIA TOD tick at end of each frame (50 Hz PAL)
        if (_vPos == 0 && _hPos == 0)
        {
            CiaA.TickTod();
            CiaB.TickTod();
        }

        // CIA timer ticks every E-clock (CPU clock / 10 ≈ 709 kHz)
        if (_hPos % 10 == 0)
        {
            CiaA.Tick();
            CiaB.Tick();
        }
    }

    private FloppyDrive? GetSelectedDrive()
    {
        byte prb = CiaB.PRB;
        if ((prb & 0x08) == 0) return Drives[0];
        if ((prb & 0x10) == 0) return Drives[1];
        if ((prb & 0x20) == 0) return Drives[2];
        if ((prb & 0x40) == 0) return Drives[3];
        return null;
    }

    public void InsertDisk(int driveNumber, AdfDisk disk)
    {
        if (driveNumber >= 0 && driveNumber < 4)
            Drives[driveNumber].InsertDisk(disk);
    }
}
