using AmigaA500.Core.Chipset;

namespace AmigaA500.Tests.Chipset;

public class CopperListBuilderTests
{
    [Fact]
    public void Move_GeneratesCorrectWords()
    {
        var list = new CopperListBuilder().Move(0x180, 0x0F00).Build();
        Assert.Equal(2, list.Length);
        Assert.Equal(0x0180, list[0]); // Register
        Assert.Equal(0x0F00, list[1]); // Value
    }

    [Fact]
    public void Wait_GeneratesCorrectWords()
    {
        var list = new CopperListBuilder().Wait(100, 0).Build();
        Assert.Equal(2, list.Length);
        Assert.Equal(0x6401, list[0]); // VP=100, HP=0, bit0=1
    }

    [Fact]
    public void End_GeneratesWaitForever()
    {
        var list = new CopperListBuilder().End().Build();
        Assert.Equal(0xFFFF, list[0]);
        Assert.Equal(0xFFFE, list[1]);
    }

    [Fact]
    public void SetColor_UsesCorrectRegister()
    {
        var list = new CopperListBuilder().SetColor(0, 0x0F00).Build();
        Assert.Equal(0x0180, list[0]); // COLOR00
        Assert.Equal(0x0F00, list[1]);
    }

    [Fact]
    public void SetColor_Index5()
    {
        var list = new CopperListBuilder().SetColor(5, 0x00FF).Build();
        Assert.Equal(0x018A, list[0]); // COLOR05 = $180 + 5*2 = $18A
    }

    [Fact]
    public void FullList_ColorBars()
    {
        var builder = new CopperListBuilder();
        for (int line = 0; line < 4; line++)
        {
            builder.WaitLine(44 + line);
            builder.SetColor(0, (ushort)(line * 0x0111));
        }
        builder.End();

        var list = builder.Build();
        // 4 lines × (WAIT + MOVE) + END = 4*4 + 2 = 18 words
        Assert.Equal(18, list.Length);
    }

    [Fact]
    public void WriteTo_WritesAllWords()
    {
        var mem = new Dictionary<uint, ushort>();
        var builder = new CopperListBuilder()
            .SetColor(0, 0x0F00)
            .End();

        builder.WriteTo((addr, val) => mem[addr] = val, 0x1000);

        Assert.Equal(4, mem.Count); // 2 instructions × 2 words
        Assert.Equal(0x0180, mem[0x1000]);
        Assert.Equal(0x0F00, mem[0x1002]);
    }

    [Fact]
    public void SizeInBytes_Correct()
    {
        var builder = new CopperListBuilder()
            .Move(0x100, 0x1200)
            .End();
        Assert.Equal(8, builder.SizeInBytes); // 4 words × 2 bytes
    }
}
