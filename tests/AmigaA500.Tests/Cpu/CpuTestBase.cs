using AmigaA500.Core.Cpu;

namespace AmigaA500.Tests.Cpu;

public abstract class CpuTestBase
{
    protected TestBus Bus;
    protected Mc68000 Cpu;
    protected const uint ProgramBase = 0x1000;

    protected CpuTestBase()
    {
        Bus = new TestBus();
        Cpu = new Mc68000(Bus);

        // Set up reset vectors
        Bus.WriteLongAt(0, 0x00FF00); // Initial SSP
        Bus.WriteLongAt(4, ProgramBase); // Initial PC

        // Initialize CPU
        Cpu.Reset();
    }

    protected void LoadAndRun(params ushort[] program)
    {
        Bus.LoadProgram(ProgramBase, program);
        Cpu.PC = ProgramBase;
        for (int i = 0; i < program.Length; i++)
            Cpu.ExecuteInstruction();
    }

    protected void LoadAndRunOne(params ushort[] program)
    {
        Bus.LoadProgram(ProgramBase, program);
        Cpu.PC = ProgramBase;
        Cpu.ExecuteInstruction();
    }
}
