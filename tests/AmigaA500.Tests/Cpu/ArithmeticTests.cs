using AmigaA500.Core.Cpu;

namespace AmigaA500.Tests.Cpu;

public class ArithmeticTests : CpuTestBase
{
    [Fact]
    public void AddQ_DataReg()
    {
        Cpu.D[0] = 10;
        // ADDQ.L #3, D0 — 0101 011 0 10 000 000 = $5680
        LoadAndRunOne(0x5680);
        Assert.Equal(13u, Cpu.D[0]);
    }

    [Fact]
    public void SubQ_DataReg()
    {
        Cpu.D[0] = 10;
        // SUBQ.L #5, D0 — 0101 101 1 10 000 000 = $5B80
        LoadAndRunOne(0x5B80);
        Assert.Equal(5u, Cpu.D[0]);
    }

    [Fact]
    public void Add_Immediate()
    {
        Cpu.D[0] = 100;
        // ADDI.W #50, D0 — 0000 011 0 01 000 000 = $0640 followed by $0032
        LoadAndRunOne(0x0640, 0x0032);
        Assert.Equal(150u, Cpu.D[0] & 0xFFFF);
    }

    [Fact]
    public void Sub_Immediate()
    {
        Cpu.D[0] = 100;
        // SUBI.W #30, D0 — 0000 010 0 01 000 000 = $0440 followed by $001E
        LoadAndRunOne(0x0440, 0x001E);
        Assert.Equal(70u, Cpu.D[0] & 0xFFFF);
    }

    [Fact]
    public void Clr_Long()
    {
        Cpu.D[0] = 0xFFFFFFFF;
        // CLR.L D0 — 0100 0010 10 000 000 = $4280
        LoadAndRunOne(0x4280);
        Assert.Equal(0u, Cpu.D[0]);
        Assert.True(Cpu.Z);
    }

    [Fact]
    public void Neg_Word()
    {
        Cpu.D[0] = 5;
        // NEG.W D0 — 0100 0100 01 000 000 = $4440
        LoadAndRunOne(0x4440);
        Assert.Equal(0xFFFBu, Cpu.D[0] & 0xFFFF);
    }

    [Fact]
    public void Mulu()
    {
        Cpu.D[0] = 100;
        Cpu.D[1] = 200;
        // MULU D1, D0 — 1100 000 011 000 001 = $C0C1
        LoadAndRunOne(0xC0C1);
        Assert.Equal(20000u, Cpu.D[0]);
    }

    [Fact]
    public void Divu()
    {
        Cpu.D[0] = 20000;
        Cpu.D[1] = 100;
        // DIVU D1, D0 — 1000 000 011 000 001 = $80C1
        LoadAndRunOne(0x80C1);
        Assert.Equal(200u, Cpu.D[0] & 0xFFFF); // Quotient
        Assert.Equal(0u, (Cpu.D[0] >> 16) & 0xFFFF); // Remainder
    }

    [Fact]
    public void Divu_WithRemainder()
    {
        Cpu.D[0] = 7;
        Cpu.D[1] = 3;
        // DIVU D1, D0
        LoadAndRunOne(0x80C1);
        Assert.Equal(2u, Cpu.D[0] & 0xFFFF); // Quotient
        Assert.Equal(1u, (Cpu.D[0] >> 16) & 0xFFFF); // Remainder
    }

    [Fact]
    public void Ext_ByteToWord()
    {
        Cpu.D[0] = 0x00FF; // -1 as byte
        // EXT.W D0 — 0100 1000 1000 0000 = $4880
        LoadAndRunOne(0x4880);
        Assert.Equal(0xFFFFu, Cpu.D[0] & 0xFFFF);
    }

    [Fact]
    public void Ext_WordToLong()
    {
        Cpu.D[0] = 0x0000FFFF; // -1 as word
        // EXT.L D0 — 0100 1000 1100 0000 = $48C0
        LoadAndRunOne(0x48C0);
        Assert.Equal(0xFFFFFFFF, Cpu.D[0]);
    }

    [Fact]
    public void Swap_DataReg()
    {
        Cpu.D[0] = 0x12340000;
        // SWAP D0 — 0100 1000 0100 0000 = $4840
        LoadAndRunOne(0x4840);
        Assert.Equal(0x00001234u, Cpu.D[0]);
    }
}
