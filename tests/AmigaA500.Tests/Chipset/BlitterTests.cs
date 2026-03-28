using AmigaA500.Core.Chipset;

namespace AmigaA500.Tests.Chipset;

public class BlitterTests
{
    private readonly byte[] _memory = new byte[65536];

    private ushort DmaRead(uint addr) => (ushort)(_memory[addr] << 8 | _memory[addr + 1]);
    private void DmaWrite(uint addr, ushort val) { _memory[addr] = (byte)(val >> 8); _memory[addr + 1] = (byte)val; }

    [Fact]
    public void Minterm_CopyA()
    {
        // Minterm $F0: D = A
        ushort result = Blitter.ApplyMinterm(0xAAAA, 0x5555, 0xFFFF, 0xF0);
        Assert.Equal(0xAAAA, result);
    }

    [Fact]
    public void Minterm_CookieCut()
    {
        // Minterm $CA: D = (A & B) | (!A & C) — masked copy
        ushort result = Blitter.ApplyMinterm(0xFF00, 0x1234, 0x5678, 0xCA);
        Assert.Equal(0x1278, result); // Top byte from B, bottom from C
    }

    [Fact]
    public void Minterm_Invert()
    {
        ushort result = Blitter.ApplyMinterm(0xAAAA, 0, 0, 0x0A);
        // Minterm $0A: D = !A & !B & C | !A & B & !C ≈ ... actually $0A is specific
        // Let's test $0F which is NOT A: ... actually simpler:
        // Minterm $30: A & !B & !C | A & !B & C = A & !B
        // Let me just use $0A = !A & B & !C (doesn't match).
        // For NOT A: minterm = 0x0F would be !A&!B&!C | !A&!B&C | !A&B&!C | !A&B&C = !A
        ushort notA = Blitter.ApplyMinterm(0xAAAA, 0, 0, 0x0F);
        // With B=0, C=0: only minterm bit 0 (all inverted) applies → !A & !B & !C = !A
        Assert.Equal(unchecked((ushort)~0xAAAA), notA);
    }

    [Fact]
    public void AreaBlit_SimpleCopy()
    {
        // Set up source data at $100
        DmaWrite(0x100, 0x1234);
        DmaWrite(0x102, 0x5678);

        var blitter = new Blitter(DmaRead, DmaWrite);
        blitter.BLTCON0 = 0x09F0; // A + D enabled, minterm = $F0 (copy A)
        blitter.FirstWordMask = 0xFFFF;
        blitter.LastWordMask = 0xFFFF;
        blitter.APt = 0x100;
        blitter.DPt = 0x200;
        blitter.AMod = 0;
        blitter.DMod = 0;

        // 1 row × 2 words
        blitter.Start((1 << 6) | 2);

        Assert.Equal(0x1234, DmaRead(0x200));
        Assert.Equal(0x5678, DmaRead(0x202));
        Assert.False(blitter.Busy);
        Assert.False(blitter.Zero);
    }

    [Fact]
    public void AreaBlit_ZeroFlag()
    {
        // Source is all zeros
        DmaWrite(0x100, 0x0000);

        var blitter = new Blitter(DmaRead, DmaWrite);
        blitter.BLTCON0 = 0x09F0; // A + D, copy A
        blitter.FirstWordMask = 0xFFFF;
        blitter.LastWordMask = 0xFFFF;
        blitter.APt = 0x100;
        blitter.DPt = 0x200;

        blitter.Start((1 << 6) | 1); // 1×1

        Assert.True(blitter.Zero);
    }

    [Fact]
    public void AreaBlit_WithModulo()
    {
        // 2 rows of 1 word each, with modulo of 2 (skip 1 word between rows)
        DmaWrite(0x100, 0xAAAA);
        DmaWrite(0x104, 0xBBBB); // Skip $102, next row at $104

        var blitter = new Blitter(DmaRead, DmaWrite);
        blitter.BLTCON0 = 0x09F0;
        blitter.FirstWordMask = 0xFFFF;
        blitter.LastWordMask = 0xFFFF;
        blitter.APt = 0x100;
        blitter.DPt = 0x200;
        blitter.AMod = 2; // Skip 2 bytes between rows
        blitter.DMod = 2;

        blitter.Start((2 << 6) | 1); // 2 rows × 1 word

        Assert.Equal(0xAAAA, DmaRead(0x200));
        Assert.Equal(0xBBBB, DmaRead(0x204));
    }

    [Fact]
    public void Completion_Callback()
    {
        bool completed = false;
        var blitter = new Blitter(DmaRead, DmaWrite, () => completed = true);
        blitter.BLTCON0 = 0x0100; // D only
        blitter.Start((1 << 6) | 1);
        Assert.True(completed);
    }
}
