using AmigaA500.Core.Cpu;

namespace AmigaA500.Tests.Cpu;

public class BranchTests : CpuTestBase
{
    [Fact]
    public void Bra_Short()
    {
        // BRA.S +4 — $6004
        LoadAndRunOne(0x6004);
        Assert.Equal(ProgramBase + 6u, Cpu.PC);
    }

    [Fact]
    public void Beq_Taken()
    {
        Cpu.Z = true;
        // BEQ.S +6 — $6706
        LoadAndRunOne(0x6706);
        Assert.Equal(ProgramBase + 8u, Cpu.PC);
    }

    [Fact]
    public void Beq_NotTaken()
    {
        Cpu.Z = false;
        // BEQ.S +6 — $6706
        LoadAndRunOne(0x6706);
        Assert.Equal(ProgramBase + 2u, Cpu.PC); // Falls through
    }

    [Fact]
    public void Bne_Taken()
    {
        Cpu.Z = false;
        // BNE.S +4 — $6604
        LoadAndRunOne(0x6604);
        Assert.Equal(ProgramBase + 6u, Cpu.PC);
    }

    [Fact]
    public void Bsr_PushesReturnAddress()
    {
        uint spBefore = Cpu.A[7];
        // BSR.S +4 — $6104
        LoadAndRunOne(0x6104);
        Assert.Equal(spBefore - 4, Cpu.A[7]);
        // Return address should be PC after BSR instruction
        uint returnAddr = (uint)(Bus.ReadWord(Cpu.A[7]) << 16) | Bus.ReadWord(Cpu.A[7] + 2);
        Assert.Equal(ProgramBase + 2u, returnAddr);
    }

    [Fact]
    public void Dbra_Loop()
    {
        Cpu.D[0] = 3; // Loop 4 times (3, 2, 1, 0), then -1 falls through
        // DBRA D0, -2 (branch back to self) — $51C8 $FFFE
        Bus.LoadProgram(ProgramBase, 0x51C8, 0xFFFE);
        Cpu.PC = ProgramBase;

        int iterations = 0;
        while (iterations < 10)
        {
            Cpu.ExecuteInstruction();
            iterations++;
            if (Cpu.PC != ProgramBase) break; // Fell through (counter = -1)
        }
        // Loops 4 times (3→2, 2→1, 1→0, 0→-1 falls through)
        Assert.Equal(4, iterations);
        Assert.Equal(0xFFFFu, Cpu.D[0] & 0xFFFF);
    }

    [Fact]
    public void Jsr_And_Rts()
    {
        // JSR $2000 — $4EB9 $0000 $2000
        // At $2000: RTS — $4E75
        Bus.LoadProgram(ProgramBase, 0x4EB9, 0x0000, 0x2000);
        Bus.LoadProgram(0x2000, 0x4E75); // RTS

        Cpu.PC = ProgramBase;
        Cpu.ExecuteInstruction(); // JSR
        Assert.Equal(0x2000u, Cpu.PC);

        Cpu.ExecuteInstruction(); // RTS
        Assert.Equal(ProgramBase + 6u, Cpu.PC); // After JSR instruction
    }
}
