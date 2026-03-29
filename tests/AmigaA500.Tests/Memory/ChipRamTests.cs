using AmigaA500.Core.Memory;

namespace AmigaA500.Tests.Memory;

public class ChipRamTests
{
    private readonly ChipRam _ram = new(512 * 1024);

    [Fact]
    public void DefaultSize_Is512KB()
    {
        Assert.Equal(512 * 1024, _ram.Size);
    }

    [Fact]
    public void ReadWrite_Byte()
    {
        _ram.WriteByte(0x1000, 0xAB);
        Assert.Equal(0xAB, _ram.ReadByte(0x1000));
    }

    [Fact]
    public void ReadWrite_Word_BigEndian()
    {
        _ram.WriteWord(0x2000, 0xBEEF);
        Assert.Equal(0xBE, _ram.ReadByte(0x2000));
        Assert.Equal(0xEF, _ram.ReadByte(0x2001));
        Assert.Equal(0xBEEF, _ram.ReadWord(0x2000));
    }

    [Fact]
    public void ReadWrite_WordAlignment_Enforced()
    {
        _ram.WriteWord(0x1001, 0x1234); // Odd address — forced to 0x1000
        Assert.Equal(0x1234, _ram.ReadWord(0x1000));
    }

    [Fact]
    public void ReadByte_OutOfRange_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _ram.ReadByte(512 * 1024));
    }

    [Fact]
    public void WriteByte_OutOfRange_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _ram.WriteByte(512 * 1024, 0xFF));
    }

    [Fact]
    public void DmaReadWrite_Word()
    {
        _ram.DmaWriteWord(0x3000, 0x1234);
        Assert.Equal(0x1234, _ram.DmaReadWord(0x3000));
    }

    [Fact]
    public void DmaReadWrite_WrapsAddress()
    {
        // Address beyond 512 KB should wrap via mask
        _ram.DmaWriteWord(512 * 1024 + 0x100, 0xCAFE);
        Assert.Equal(0xCAFE, _ram.DmaReadWord(0x100));
    }

    [Fact]
    public void DmaReadByte_ReturnsCorrectByte()
    {
        _ram.WriteWord(0x4000, 0xDEAD);
        Assert.Equal(0xDE, _ram.DmaReadByte(0x4000));
        Assert.Equal(0xAD, _ram.DmaReadByte(0x4001));
    }

    [Fact]
    public void BulkWrite_CopiesData()
    {
        byte[] src = { 1, 2, 3, 4, 5 };
        _ram.BulkWrite(0x5000, src);

        for (int i = 0; i < src.Length; i++)
            Assert.Equal(src[i], _ram.ReadByte((uint)(0x5000 + i)));
    }

    [Fact]
    public void BulkWrite_WrapsAddress()
    {
        byte[] src = { 0xAA, 0xBB };
        uint addr = (uint)(512 * 1024 - 1); // Near end, wraps for second byte
        _ram.BulkWrite(addr, src);
        Assert.Equal(0xAA, _ram.ReadByte(addr & (uint)(512 * 1024 - 1)));
    }

    [Fact]
    public void Clear_ZerosAllBytes()
    {
        _ram.WriteWord(0x0, 0xFFFF);
        _ram.WriteWord(0x100, 0xABCD);
        _ram.Clear();

        Assert.Equal(0x0000, _ram.ReadWord(0x0));
        Assert.Equal(0x0000, _ram.ReadWord(0x100));
    }

    [Fact]
    public void Constructor_NonPowerOfTwo_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ChipRam(500 * 1024));
    }

    [Fact]
    public void Constructor_1MB_Supported()
    {
        var mb = new ChipRam(1024 * 1024);
        Assert.Equal(1024 * 1024, mb.Size);
        mb.WriteWord(0xFFFFC, 0x1234);
        Assert.Equal(0x1234, mb.ReadWord(0xFFFFC));
    }

    [Fact]
    public void AsSpan_ReturnsFullBuffer()
    {
        var span = _ram.AsSpan();
        Assert.Equal(_ram.Size, span.Length);
    }
}
