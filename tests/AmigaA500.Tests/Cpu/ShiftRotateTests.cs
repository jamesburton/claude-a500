using AmigaA500.Core.Cpu;

namespace AmigaA500.Tests.Cpu;

public class ShiftRotateTests : CpuTestBase
{
    [Fact]
    public void Asr_Preserves_SignBit()
    {
        Cpu.D[0] = 0x8000; // Negative word
        Cpu.D[1] = 1;
        // ASR.W D1, D0 — $E260 (register count, word)
        LoadAndRunOne(0xE260);
        Assert.Equal(0xC000u, Cpu.D[0] & 0xFFFF); // Sign preserved
        Assert.True(Cpu.N);
    }

    [Fact]
    public void Lsr_ZeroFills()
    {
        Cpu.D[0] = 0x8000;
        Cpu.D[1] = 1;
        // LSR.W D1, D0 — $E268
        LoadAndRunOne(0xE268);
        Assert.Equal(0x4000u, Cpu.D[0] & 0xFFFF); // Zero filled
        Assert.False(Cpu.N);
    }

    [Fact]
    public void Rol_Wraps()
    {
        Cpu.D[0] = 0x80000001;
        // ROL.L #1, D0 — $E398
        LoadAndRunOne(0xE398);
        Assert.Equal(0x00000003u, Cpu.D[0]);
        Assert.True(Cpu.C);
    }

    [Fact]
    public void Ror_Wraps()
    {
        Cpu.D[0] = 0x00000001;
        // ROR.L #1, D0 — $E298
        LoadAndRunOne(0xE298);
        Assert.Equal(0x80000000u, Cpu.D[0]);
        Assert.True(Cpu.C);
    }

    [Fact]
    public void Lsl_Immediate8()
    {
        Cpu.D[0] = 1;
        // LSL.L #8, D0 — $E188 (count 0 = 8)
        LoadAndRunOne(0xE188);
        Assert.Equal(0x100u, Cpu.D[0]);
    }

    [Fact]
    public void Shift_Count0_NoChange()
    {
        Cpu.D[0] = 0x1234;
        Cpu.D[1] = 0; // Count = 0
        // LSL.W D1, D0 — $E368
        LoadAndRunOne(0xE368);
        Assert.Equal(0x1234u, Cpu.D[0] & 0xFFFF);
        Assert.False(Cpu.C);
    }
}
