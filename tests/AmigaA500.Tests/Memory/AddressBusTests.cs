using AmigaA500.Core;
using AmigaA500.Core.Cia;
using AmigaA500.Core.Chipset;
using AmigaA500.Core.Memory;

namespace AmigaA500.Tests.Memory;

public class AddressBusTests
{
    private readonly byte[] _rom;
    private readonly AddressBus _bus;
    private readonly CustomRegisters _custom;
    private readonly Cia8520 _ciaA, _ciaB;

    public AddressBusTests()
    {
        _rom = new byte[256 * 1024];
        _rom[0] = 0xDE; _rom[1] = 0xAD; // Marker at ROM start
        _custom = new CustomRegisters();
        _ciaA = new Cia8520();
        _ciaB = new Cia8520();
        _bus = new AddressBus(512 * 1024, _rom, _custom, _ciaA, _ciaB);
    }

    [Fact]
    public void ChipRam_ReadWrite()
    {
        _bus.Overlay = false;
        _bus.WriteWord(0x1000, 0x1234);
        Assert.Equal(0x1234, _bus.ReadWord(0x1000));
    }

    [Fact]
    public void ChipRam_ByteAccess()
    {
        _bus.Overlay = false;
        _bus.WriteByte(0x2000, 0xAB);
        Assert.Equal(0xAB, _bus.ReadByte(0x2000));
    }

    [Fact]
    public void Overlay_ReadsRomFromLowMemory()
    {
        Assert.True(_bus.Overlay);
        // ROM data at $FC0000 should be visible at $000000
        Assert.Equal(0xDE, _bus.ReadByte(0x000000));
        Assert.Equal(0xAD, _bus.ReadByte(0x000001));
    }

    [Fact]
    public void Overlay_DisableReadsChipRam()
    {
        _bus.Overlay = false;
        _bus.WriteByte(0x000000, 0x42);
        Assert.Equal(0x42, _bus.ReadByte(0x000000));
    }

    [Fact]
    public void Rom_ReadableAtFC0000()
    {
        Assert.Equal(0xDE, _bus.ReadByte(0xFC0000));
    }

    [Fact]
    public void Rom_WritesIgnored()
    {
        _bus.WriteByte(0xFC0000, 0x00);
        Assert.Equal(0xDE, _bus.ReadByte(0xFC0000)); // Unchanged
    }

    [Fact]
    public void CustomRegisters_Accessible()
    {
        // Write to COLOR00
        _bus.WriteWord(0xDFF180, 0x0F00);
        Assert.Equal(0x0F00, _custom.Color[0]);
    }

    [Fact]
    public void CustomRegisters_ReadDMACONR()
    {
        _custom.WriteRegister(0x096, 0x8201); // Set some DMA bits
        ushort val = _bus.ReadWord(0xDFF002);
        Assert.Equal(0x0201, val);
    }

    [Fact]
    public void CiaA_OddBytesOnly()
    {
        _ciaA.DDRA = 0xFF;
        _ciaA.PRA = 0x42;
        byte val = _bus.ReadByte(0xBFE001);
        Assert.Equal(0x42, val);
    }

    [Fact]
    public void CiaB_EvenBytesOnly()
    {
        _ciaB.DDRA = 0xFF;
        _ciaB.PRA = 0x99;
        byte val = _bus.ReadByte(0xBFD000);
        Assert.Equal(0x99, val);
    }

    [Fact]
    public void DmaAccess_DirectChipRam()
    {
        _bus.DmaWriteWord(0x5000, 0xBEEF);
        Assert.Equal(0xBEEF, _bus.DmaReadWord(0x5000));
    }

    [Fact]
    public void DmaAccess_Wraps512KB()
    {
        _bus.DmaWriteWord(0x7FFFE, 0x1234);
        Assert.Equal(0x1234, _bus.DmaReadWord(0x7FFFE));
    }

    [Fact]
    public void SlowRam_NotAvailableByDefault()
    {
        ushort val = _bus.ReadWord(0xC00000);
        Assert.Equal(0xFFFF, val); // Open bus
    }

    [Fact]
    public void SlowRam_EnabledWorks()
    {
        _bus.EnableSlowRam();
        _bus.WriteWord(0xC00000, 0xAAAA);
        Assert.Equal(0xAAAA, _bus.ReadWord(0xC00000));
    }

    [Fact]
    public void Address_Wraps24Bit()
    {
        _bus.Overlay = false;
        _bus.WriteWord(0x1000000 + 0x1000, 0x5678); // Address > 24 bits
        Assert.Equal(0x5678, _bus.ReadWord(0x1000)); // Should wrap
    }
}
