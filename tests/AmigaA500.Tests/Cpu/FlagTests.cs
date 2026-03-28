using AmigaA500.Core.Cpu;

namespace AmigaA500.Tests.Cpu;

public class FlagTests : CpuTestBase
{
    [Fact]
    public void Add_Carry()
    {
        Cpu.D[0] = 0xFFFF;
        // ADDI.W #1, D0
        LoadAndRunOne(0x0640, 0x0001);
        Assert.True(Cpu.C);
        Assert.True(Cpu.X);
        Assert.True(Cpu.Z); // Result is 0
    }

    [Fact]
    public void Sub_Borrow()
    {
        Cpu.D[0] = 0x0000;
        // SUBI.W #1, D0
        LoadAndRunOne(0x0440, 0x0001);
        Assert.True(Cpu.C); // Borrow
        Assert.True(Cpu.N); // Negative result
    }

    [Fact]
    public void Cmp_DoesNotAffectX()
    {
        Cpu.X = true;
        Cpu.D[0] = 5;
        Cpu.D[1] = 10;
        // CMP.L D1, D0
        LoadAndRunOne(0xB081);
        Assert.True(Cpu.X); // X unchanged by CMP
    }

    [Fact]
    public void Move_ClearsVC()
    {
        Cpu.V = true;
        Cpu.C = true;
        Cpu.D[0] = 0x80000000;
        // MOVE.L D0, D1
        LoadAndRunOne(0x2200);
        Assert.False(Cpu.V);
        Assert.False(Cpu.C);
        Assert.True(Cpu.N);
    }

    [Fact]
    public void Scc_SetsByte()
    {
        Cpu.Z = true;
        // SEQ D0 — $57C0
        LoadAndRunOne(0x57C0);
        Assert.Equal(0xFFu, Cpu.D[0] & 0xFF); // True = $FF
    }

    [Fact]
    public void Scc_ClearsByte()
    {
        Cpu.Z = false;
        // SEQ D0 — $57C0
        LoadAndRunOne(0x57C0);
        Assert.Equal(0x00u, Cpu.D[0] & 0xFF); // False = $00
    }
}
