using AmigaA500.Core.Cpu;

namespace AmigaA500.Tests.Cpu;

public class MoveTests : CpuTestBase
{
    [Fact]
    public void MoveQ_PositiveValue()
    {
        // MOVEQ #42, D0
        LoadAndRunOne(0x702A); // 0111 000 0 00101010
        Assert.Equal(42u, Cpu.D[0]);
        Assert.False(Cpu.N);
        Assert.False(Cpu.Z);
    }

    [Fact]
    public void MoveQ_NegativeValue()
    {
        // MOVEQ #-1, D3
        LoadAndRunOne(0x76FF); // 0111 011 0 11111111
        Assert.Equal(0xFFFFFFFF, Cpu.D[3]);
        Assert.True(Cpu.N);
        Assert.False(Cpu.Z);
    }

    [Fact]
    public void MoveQ_Zero()
    {
        Cpu.D[0] = 0x12345678;
        // MOVEQ #0, D0
        LoadAndRunOne(0x7000);
        Assert.Equal(0u, Cpu.D[0]);
        Assert.True(Cpu.Z);
        Assert.False(Cpu.N);
    }

    [Fact]
    public void MoveW_DataRegToDataReg()
    {
        Cpu.D[1] = 0xABCD;
        // MOVE.W D1, D0 — opcode: 0011 000 000 000 001 = $3001
        LoadAndRunOne(0x3001);
        Assert.Equal(0xABCDu, Cpu.D[0] & 0xFFFF);
    }

    [Fact]
    public void MoveL_DataRegToDataReg()
    {
        Cpu.D[2] = 0xDEADBEEF;
        // MOVE.L D2, D0 — opcode: 0010 000 000 000 010 = $2002
        LoadAndRunOne(0x2002);
        Assert.Equal(0xDEADBEEF, Cpu.D[0]);
    }

    [Fact]
    public void MoveL_Immediate()
    {
        // MOVE.L #$12345678, D0 — 0x203C followed by $1234, $5678
        LoadAndRunOne(0x203C, 0x1234, 0x5678);
        Assert.Equal(0x12345678u, Cpu.D[0]);
    }

    [Fact]
    public void MoveW_ToMemory_Indirect()
    {
        Cpu.D[0] = 0x00FF;
        Cpu.A[1] = 0x2000;
        // MOVE.W D0, (A1) — 0011 001 010 000 000 = $3280
        LoadAndRunOne(0x3280);
        Assert.Equal(0x00FF, Bus.ReadWord(0x2000));
    }

    [Fact]
    public void MoveW_FromMemory_Indirect()
    {
        Bus.WriteWordAt(0x3000, 0xBEEF);
        Cpu.A[2] = 0x3000;
        // MOVE.W (A2), D0 — 0011 000 000 010 010 = $3012
        LoadAndRunOne(0x3012);
        Assert.Equal(0xBEEFu, Cpu.D[0] & 0xFFFF);
    }

    [Fact]
    public void MoveW_PostIncrement()
    {
        Bus.WriteWordAt(0x4000, 0x1111);
        Bus.WriteWordAt(0x4002, 0x2222);
        Cpu.A[3] = 0x4000;
        // MOVE.W (A3)+, D0 — 0011 000 000 011 011 = $301B
        LoadAndRunOne(0x301B);
        Assert.Equal(0x1111u, Cpu.D[0] & 0xFFFF);
        Assert.Equal(0x4002u, Cpu.A[3]);
    }

    [Fact]
    public void MoveW_PreDecrement()
    {
        Bus.WriteWordAt(0x4FFE, 0x9999);
        Cpu.A[4] = 0x5000;
        // MOVE.W -(A4), D0 — 0011 000 000 100 100 = $3024
        LoadAndRunOne(0x3024);
        Assert.Equal(0x9999u, Cpu.D[0] & 0xFFFF);
        Assert.Equal(0x4FFEu, Cpu.A[4]);
    }

    [Fact]
    public void MoveA_W_SignExtends()
    {
        Cpu.D[0] = 0xFF00;
        // MOVEA.W D0, A1 — 0011 001 001 000 000 = $3240
        LoadAndRunOne(0x3240);
        Assert.Equal(0xFFFFFF00, Cpu.A[1]); // Sign-extended
    }
}
