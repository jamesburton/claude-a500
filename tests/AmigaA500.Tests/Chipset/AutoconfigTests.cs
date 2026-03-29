using AmigaA500.Core.Chipset;

namespace AmigaA500.Tests.Chipset;

public class AutoconfigTests
{
    [Fact]
    public void AddBoard_IncreasesCount()
    {
        var ac = new Autoconfig();
        ac.AddBoard(new ExpansionBoard { SizeCode = 4, ProductId = 1 });
        Assert.Equal(1, ac.BoardCount);
    }

    [Fact]
    public void ReadByte_ReturnsTypeAndSize()
    {
        var ac = new Autoconfig();
        ac.AddBoard(new ExpansionBoard { Type = 0xC0, SizeCode = 4 });
        byte val = ac.ReadByte(0xE80000);
        Assert.Equal(0xC4, val); // Type $C0 | Size 4
    }

    [Fact]
    public void Configure_AdvancesToNextBoard()
    {
        var ac = new Autoconfig();
        ac.AddBoard(new ExpansionBoard());
        ac.AddBoard(new ExpansionBoard());

        ac.WriteByte(0xE80048, 0x20); // Configure first board at $200000
        Assert.Equal(1, ac.ConfiguredCount);
    }

    [Fact]
    public void Shutup_SkipsBoard()
    {
        var ac = new Autoconfig();
        ac.AddBoard(new ExpansionBoard());
        ac.WriteByte(0xE8004C, 0x00); // Shutup
        Assert.Equal(0, ac.ConfiguredCount);
    }

    [Fact]
    public void SizeCode_MapsCorrectly()
    {
        Assert.Equal(512 * 1024, new ExpansionBoard { SizeCode = 4 }.SizeInBytes);
        Assert.Equal(1024 * 1024, new ExpansionBoard { SizeCode = 5 }.SizeInBytes);
        Assert.Equal(2 * 1024 * 1024, new ExpansionBoard { SizeCode = 6 }.SizeInBytes);
    }
}
