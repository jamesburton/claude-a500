using AmigaA500.Core.Cpu;

namespace AmigaA500.Tests.Cpu;

public class AddressingModeTests : CpuTestBase
{
    [Fact]
    public void Displacement_d16An()
    {
        Bus.WriteWordAt(0x3010, 0xBEEF);
        Cpu.A[0] = 0x3000;
        // MOVE.W $10(A0), D0 — $3028 $0010
        LoadAndRunOne(0x3028, 0x0010);
        Assert.Equal(0xBEEFu, Cpu.D[0] & 0xFFFF);
    }

    [Fact]
    public void AbsoluteShort()
    {
        Bus.WriteWordAt(0x1234, 0xCAFE);
        // MOVE.W $1234.W, D0 — $3038 $1234
        LoadAndRunOne(0x3038, 0x1234);
        Assert.Equal(0xCAFEu, Cpu.D[0] & 0xFFFF);
    }

    [Fact]
    public void AbsoluteLong()
    {
        Bus.WriteWordAt(0x5678, 0xFACE);
        // MOVE.W $5678.L, D0 — $3039 $0000 $5678
        LoadAndRunOne(0x3039, 0x0000, 0x5678);
        Assert.Equal(0xFACEu, Cpu.D[0] & 0xFFFF);
    }

    [Fact]
    public void Immediate_Word()
    {
        // MOVE.W #$1234, D0 — $303C $1234
        LoadAndRunOne(0x303C, 0x1234);
        Assert.Equal(0x1234u, Cpu.D[0] & 0xFFFF);
    }

    [Fact]
    public void Immediate_Byte()
    {
        // MOVE.B #$AB, D0 — $103C $00AB
        LoadAndRunOne(0x103C, 0x00AB);
        Assert.Equal(0xABu, Cpu.D[0] & 0xFF);
    }

    [Fact]
    public void Movem_SaveRegisters()
    {
        Cpu.D[0] = 0x11111111;
        Cpu.D[1] = 0x22222222;
        Cpu.A[0] = 0x33333333;
        Cpu.A[7] = 0xF000;

        // MOVEM.L D0-D1/A0, -(A7) — $48E7 $C080
        LoadAndRunOne(0x48E7, 0xC080);

        // Pre-decrement stores in reverse: A0 first (highest address), then D1, D0
        uint sp = Cpu.A[7];
        uint d0 = (uint)(Bus.ReadWord(sp) << 16) | Bus.ReadWord(sp + 2);
        uint d1 = (uint)(Bus.ReadWord(sp + 4) << 16) | Bus.ReadWord(sp + 6);
        uint a0 = (uint)(Bus.ReadWord(sp + 8) << 16) | Bus.ReadWord(sp + 10);

        Assert.Equal(0x11111111u, d0);
        Assert.Equal(0x22222222u, d1);
        Assert.Equal(0x33333333u, a0);
    }

    [Fact]
    public void Link_And_Unlk()
    {
        Cpu.A[6] = 0x12345678;
        Cpu.A[7] = 0xF000;

        // LINK A6, #-8 — $4E56 $FFF8
        LoadAndRunOne(0x4E56, 0xFFF8);

        uint savedFP = (uint)(Bus.ReadWord(0xEFFC) << 16) | Bus.ReadWord(0xEFFE);
        Assert.Equal(0x12345678u, savedFP); // Old A6 pushed
        Assert.Equal(0xEFFCu, Cpu.A[6]); // A6 = SP after push
        Assert.Equal(0xEFF4u, Cpu.A[7]); // SP = A6 + displacement (-8)

        // UNLK A6 — $4E5E
        LoadAndRunOne(0x4E5E);
        Assert.Equal(0x12345678u, Cpu.A[6]); // Restored
    }

    [Fact]
    public void Lea_Displacement()
    {
        Cpu.A[0] = 0x5000;
        // LEA $20(A0), A1 — $43E8 $0020
        LoadAndRunOne(0x43E8, 0x0020);
        Assert.Equal(0x5020u, Cpu.A[1]);
    }

    [Fact]
    public void Lea_AbsoluteLong()
    {
        // LEA $00012000, A0 — $41F9 $0001 $2000
        LoadAndRunOne(0x41F9, 0x0000, 0x5000);
        Assert.Equal(0x00005000u, Cpu.A[0]);
    }
}
