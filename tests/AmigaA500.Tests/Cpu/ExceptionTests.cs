using AmigaA500.Core.Cpu;

namespace AmigaA500.Tests.Cpu;

public class ExceptionTests : CpuTestBase
{
    [Fact]
    public void Trap_RaisesException()
    {
        // Set TRAP #0 vector at $80
        Bus.WriteLongAt(0x80, 0x5000);
        Bus.LoadProgram(0x5000, 0x4E71);

        uint spBefore = Cpu.A[7];
        LoadAndRunOne(0x4E40);

        Assert.Equal(0x5000u, Cpu.PC);
        Assert.True(Cpu.Supervisor);
        Assert.True(Cpu.A[7] < spBefore);
    }

    [Fact]
    public void Rte_RestoresState()
    {
        // Set up stack frame: SR then PC
        Bus.WriteWordAt(0xEFF0, 0x2000); // Saved SR (supervisor, no flags)
        Bus.WriteLongAt(0xEFF2, 0x2000); // Return PC
        Cpu.A[7] = 0xEFF0;

        LoadAndRunOne(0x4E73);
        Assert.Equal(0x2000u, Cpu.PC);
    }

    [Fact]
    public void IllegalInstruction_Traps()
    {
        // Line-A vector at $28 (vector 10)
        Bus.WriteLongAt(0x28, 0x6000);
        Bus.LoadProgram(0x6000, 0x4E71);

        LoadAndRunOne(0xA000);
        // After exception, PC should be at handler (exception sets PC, doesn't execute)
        Assert.Equal(0x6000u, Cpu.PC);
    }

    [Fact]
    public void DivisionByZero_Traps()
    {
        // Divide-by-zero vector at $14 (vector 5)
        Bus.WriteLongAt(0x14, 0x7000);
        Bus.LoadProgram(0x7000, 0x4E71);

        Cpu.D[0] = 100;
        Cpu.D[1] = 0;
        LoadAndRunOne(0x80C1);
        Assert.Equal(0x7000u, Cpu.PC);
    }

    [Fact]
    public void Stop_HaltsCpu()
    {
        LoadAndRunOne(0x4E72, 0x2700);
        Assert.True(Cpu.Halted);
    }

    [Fact]
    public void Interrupt_UnhaltsStop()
    {
        Cpu.Halted = true;
        Cpu.InterruptMask = 0; // Allow all interrupts
        // Level 2 auto-vector at $68
        Bus.WriteLongAt(0x68, 0x8000);
        Bus.LoadProgram(0x8000, 0x4E71);

        Cpu.RaiseInterrupt(2);
        Assert.False(Cpu.Halted);
        Assert.Equal(0x8000u, Cpu.PC);
    }

    [Fact]
    public void Reset_InitializesFromVectors()
    {
        Bus.WriteLongAt(0, 0x00FF00);
        Bus.WriteLongAt(4, 0x001000);

        Cpu.Reset();
        Assert.Equal(0x00FF00u, Cpu.A[7]);
        Assert.Equal(0x001000u, Cpu.PC);
        Assert.True(Cpu.Supervisor);
        Assert.Equal(7, Cpu.InterruptMask);
    }
}
