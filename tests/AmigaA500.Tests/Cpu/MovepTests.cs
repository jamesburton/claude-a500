using AmigaA500.Core.Cpu;

namespace AmigaA500.Tests.Cpu;

public class MovepTests : CpuTestBase
{
    [Fact]
    public void Movep_WordToMemory()
    {
        Cpu.D[0] = 0x1234;
        Cpu.A[0] = 0x2000;
        // MOVEP.W D0, 0(A0) — $0188 $0000
        LoadAndRunOne(0x0188, 0x0000);
        Assert.Equal(0x12, Bus.ReadByte(0x2000));
        Assert.Equal(0x34, Bus.ReadByte(0x2002));
    }

    [Fact]
    public void Movep_LongToMemory()
    {
        Cpu.D[0] = 0xDEADBEEF;
        Cpu.A[0] = 0x3000;
        // MOVEP.L D0, 0(A0) — $01C8 $0000
        LoadAndRunOne(0x01C8, 0x0000);
        Assert.Equal(0xDE, Bus.ReadByte(0x3000));
        Assert.Equal(0xAD, Bus.ReadByte(0x3002));
        Assert.Equal(0xBE, Bus.ReadByte(0x3004));
        Assert.Equal(0xEF, Bus.ReadByte(0x3006));
    }

    [Fact]
    public void Movep_WordFromMemory()
    {
        Bus.WriteByte(0x4000, 0xAB);
        Bus.WriteByte(0x4002, 0xCD);
        Cpu.A[0] = 0x4000;
        Cpu.D[0] = 0;
        // MOVEP.W 0(A0), D0 — $0108 $0000
        LoadAndRunOne(0x0108, 0x0000);
        Assert.Equal(0xABCDu, Cpu.D[0] & 0xFFFF);
    }

    [Fact]
    public void Movep_LongFromMemory()
    {
        Bus.WriteByte(0x5000, 0x11);
        Bus.WriteByte(0x5002, 0x22);
        Bus.WriteByte(0x5004, 0x33);
        Bus.WriteByte(0x5006, 0x44);
        Cpu.A[0] = 0x5000;
        Cpu.D[0] = 0;
        // MOVEP.L 0(A0), D0 — $0148 $0000
        LoadAndRunOne(0x0148, 0x0000);
        Assert.Equal(0x11223344u, Cpu.D[0]);
    }

    [Fact]
    public void Movep_WithDisplacement()
    {
        Cpu.D[0] = 0xCAFE;
        Cpu.A[1] = 0x1000;
        // MOVEP.W D0, $10(A1) — $0189 $0010
        LoadAndRunOne(0x0189, 0x0010);
        Assert.Equal(0xCA, Bus.ReadByte(0x1010));
        Assert.Equal(0xFE, Bus.ReadByte(0x1012));
    }
}
