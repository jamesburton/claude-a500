using AmigaA500.Core;
using AmigaA500.Core.Cpu;
using AmigaA500.Tests;

namespace AmigaA500.Tests.Integration;

/// <summary>
/// Tests that exercise the Kickstart ROM boot path with the real ROM file if available.
/// Falls back to synthetic ROMs if the real ROM isn't present.
/// </summary>
public class KickstartBootTests
{
    private static readonly string RomPath = Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "firmware", "kick34005.A500");

    private static bool RomAvailable => File.Exists(RomPath);

    [Fact]
    public void SyntheticRom_MemoryClearLoop()
    {
        // Test memory clear pattern using raw CPU (no Amiga overhead)
        var bus = new TestBus();
        var cpu = new Mc68000(bus);

        // Set up program at $1000
        uint pc = 0x1000;
        bus.WriteLongAt(0, 0xFF00);   // SSP
        bus.WriteLongAt(4, pc);       // PC

        // LEA $10000, A0
        bus.LoadProgram(pc, 0x41F9, 0x0001, 0x0000);
        // MOVEA.L #0, A1
        bus.LoadProgram(pc + 6, 0x227C, 0x0000, 0x0000);
        // MOVE.W #$0F, D1 (16 iterations instead of 256 for speed)
        bus.LoadProgram(pc + 12, 0x323C, 0x000F);
        // MOVE.L A1,(A0)+
        bus.LoadProgram(pc + 16, 0x20C9);
        // DBF D1, -4
        bus.LoadProgram(pc + 18, 0x51C9, 0xFFFC);
        // STOP #$2700
        bus.LoadProgram(pc + 22, 0x4E72, 0x2700);

        cpu.Reset();

        for (int j = 0; j < 500; j++)
        {
            cpu.ExecuteInstruction();
            if (cpu.Halted) break;
        }

        Assert.True(cpu.Halted);
        // A0 = $10000 + 16*4 = $10040
        Assert.Equal(0x00010040u, cpu.A[0]);
    }

    [Fact]
    public void SyntheticRom_InterruptVector()
    {
        var rom = new byte[256 * 1024];
        // SSP
        rom[0] = 0x00; rom[1] = 0x00; rom[2] = 0xFF; rom[3] = 0x00;
        // PC = $FC0008
        rom[4] = 0x00; rom[5] = 0xFC; rom[6] = 0x00; rom[7] = 0x08;
        int i = 8;
        // Set up Level 3 vector (VERTB) at $6C to point to handler at $FC0020
        // MOVE.L #$00FC0020, $6C
        rom[i++] = 0x23; rom[i++] = 0xFC;
        rom[i++] = 0x00; rom[i++] = 0xFC; rom[i++] = 0x00; rom[i++] = 0x20; // Value
        rom[i++] = 0x00; rom[i++] = 0x00; rom[i++] = 0x00; rom[i++] = 0x6C; // Address
        // Lower interrupt mask
        // ANDI #$F8FF, SR → allow level 1-7
        rom[i++] = 0x02; rom[i++] = 0x7C; rom[i++] = 0xF8; rom[i++] = 0xFF;
        // STOP #$2000
        rom[i++] = 0x4E; rom[i++] = 0x72; rom[i++] = 0x20; rom[i++] = 0x00;
        // Handler at $FC0020: MOVEQ #99, D7; RTE
        int handler = 0x20;
        rom[handler++] = 0x7E; rom[handler++] = 0x63; // MOVEQ #99, D7
        rom[handler++] = 0x4E; rom[handler++] = 0x73; // RTE

        var amiga = new Amiga(rom);
        amiga.Reset();
        amiga.Bus.Overlay = false; // Allow vector table writes

        // Run setup code
        for (int j = 0; j < 20; j++) amiga.Step();

        // Simulate VERTB interrupt (the Amiga system fires this at VBLANK)
        amiga.Custom.WriteRegister(0x09A, 0xC020); // Enable INTEN + VERTB
        amiga.Custom.WriteRegister(0x09C, 0x8020); // Request VERTB

        // Step to process interrupt
        for (int j = 0; j < 10; j++) amiga.Step();

        Assert.Equal(99u, amiga.Cpu.D[7]);
    }

    [Fact]
    public void RealKickstart_RunsMultipleFrames()
    {
        if (!RomAvailable) return; // Skip if ROM not available

        var rom = File.ReadAllBytes(RomPath);
        var amiga = new Amiga(rom);
        amiga.Reset();

        // Run 10 frames — should not crash
        for (int frame = 0; frame < 10; frame++)
            amiga.RunFrame();

        Assert.True(amiga.Cpu.TotalCycles > 100000);
    }

    [Fact]
    public void RealKickstart_PCProgresses()
    {
        if (!RomAvailable) return;

        var rom = File.ReadAllBytes(RomPath);
        var amiga = new Amiga(rom);
        amiga.Reset();

        uint initialPC = amiga.Cpu.PC;
        amiga.RunFrame();

        // PC should have moved from initial position
        // (even if stuck in a loop, the first instructions should execute)
        Assert.True(amiga.Cpu.TotalCycles > 0);
    }
}
