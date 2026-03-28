using AmigaA500.Core.Cpu;

namespace AmigaA500.Tests.Cpu;

public class LogicTests : CpuTestBase
{
    [Fact]
    public void And_Immediate()
    {
        Cpu.D[0] = 0xFF;
        // ANDI.B #$0F, D0 — $0200 $000F
        LoadAndRunOne(0x0200, 0x000F);
        Assert.Equal(0x0Fu, Cpu.D[0] & 0xFF);
    }

    [Fact]
    public void Or_Immediate()
    {
        Cpu.D[0] = 0xF0;
        // ORI.B #$0F, D0 — $0000 $000F
        LoadAndRunOne(0x0000, 0x000F);
        Assert.Equal(0xFFu, Cpu.D[0] & 0xFF);
    }

    [Fact]
    public void Eor_Immediate()
    {
        Cpu.D[0] = 0xFF;
        // EORI.B #$0F, D0 — $0A00 $000F
        LoadAndRunOne(0x0A00, 0x000F);
        Assert.Equal(0xF0u, Cpu.D[0] & 0xFF);
    }

    [Fact]
    public void Not_Long()
    {
        Cpu.D[0] = 0x00000000;
        // NOT.L D0 — $4680
        LoadAndRunOne(0x4680);
        Assert.Equal(0xFFFFFFFF, Cpu.D[0]);
    }

    [Fact]
    public void Tst_Zero()
    {
        Cpu.D[0] = 0;
        // TST.L D0 — $4A80
        LoadAndRunOne(0x4A80);
        Assert.True(Cpu.Z);
        Assert.False(Cpu.N);
    }

    [Fact]
    public void Tst_Negative()
    {
        Cpu.D[0] = 0x80000000;
        // TST.L D0 — $4A80
        LoadAndRunOne(0x4A80);
        Assert.False(Cpu.Z);
        Assert.True(Cpu.N);
    }

    [Fact]
    public void Btst_Set()
    {
        Cpu.D[0] = 0x80;
        // BTST #7, D0 — $0800 $0007
        LoadAndRunOne(0x0800, 0x0007);
        Assert.False(Cpu.Z); // Bit is set → Z = 0
    }

    [Fact]
    public void Btst_Clear()
    {
        Cpu.D[0] = 0x00;
        // BTST #7, D0 — $0800 $0007
        LoadAndRunOne(0x0800, 0x0007);
        Assert.True(Cpu.Z); // Bit is clear → Z = 1
    }

    [Fact]
    public void Cmp_Equal()
    {
        Cpu.D[0] = 42;
        Cpu.D[1] = 42;
        // CMP.L D1, D0 — $B081
        LoadAndRunOne(0xB081);
        Assert.True(Cpu.Z);
    }

    [Fact]
    public void Cmp_Greater()
    {
        Cpu.D[0] = 100;
        Cpu.D[1] = 50;
        // CMP.L D1, D0 — $B081
        LoadAndRunOne(0xB081);
        Assert.False(Cpu.Z);
        Assert.False(Cpu.N); // D0 - D1 = 50 (positive)
    }

    [Fact]
    public void Lsl_Register()
    {
        Cpu.D[0] = 1;
        Cpu.D[1] = 4;
        // LSL.L D1, D0 — $E3A8
        LoadAndRunOne(0xE3A8);
        Assert.Equal(16u, Cpu.D[0]);
    }

    [Fact]
    public void Lsr_Immediate()
    {
        Cpu.D[0] = 16;
        // LSR.L #4, D0 — $E888
        LoadAndRunOne(0xE888);
        Assert.Equal(1u, Cpu.D[0]);
    }
}
