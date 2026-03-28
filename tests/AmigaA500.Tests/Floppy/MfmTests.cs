using AmigaA500.Core.Floppy;

namespace AmigaA500.Tests.Floppy;

public class MfmTests
{
    [Fact]
    public void Encode_Decode_Roundtrip()
    {
        byte[] data = { 0xAA, 0x55, 0xFF, 0x00 };
        byte[] encoded = MfmCodec.Encode(data);
        byte[] decoded = MfmCodec.Decode(encoded);
        Assert.Equal(data, decoded);
    }

    [Fact]
    public void Encode_DoublesSize()
    {
        byte[] data = { 0x12, 0x34 };
        byte[] encoded = MfmCodec.Encode(data);
        Assert.Equal(4, encoded.Length);
    }

    [Fact]
    public void SyncWord_Constant()
    {
        Assert.Equal(0x4489, MfmCodec.SyncWord);
    }

    [Fact]
    public void DiskDma_RequiresDoubleWrite()
    {
        var mem = new byte[65536];
        var dma = new DiskDma(
            addr => (ushort)(mem[addr] << 8 | mem[addr + 1]),
            (addr, val) => { mem[addr] = (byte)(val >> 8); mem[addr + 1] = (byte)val; }
        );

        dma.WriteDskLen(0x8010); // First write
        Assert.False(dma.Active);

        dma.WriteDskLen(0x8010); // Second write — activates
        Assert.True(dma.Active);
    }

    [Fact]
    public void DiskDma_DisablesOnClear()
    {
        var mem = new byte[65536];
        var dma = new DiskDma(
            addr => (ushort)(mem[addr] << 8 | mem[addr + 1]),
            (addr, val) => { mem[addr] = (byte)(val >> 8); mem[addr + 1] = (byte)val; }
        );

        dma.WriteDskLen(0x8010);
        dma.WriteDskLen(0x8010);
        Assert.True(dma.Active);

        dma.WriteDskLen(0x0000); // Disable
        Assert.False(dma.Active);
    }

    [Fact]
    public void DiskDma_CompletionCallback()
    {
        var mem = new byte[65536];
        bool completed = false;
        var dma = new DiskDma(
            addr => (ushort)(mem[addr] << 8 | mem[addr + 1]),
            (addr, val) => { mem[addr] = (byte)(val >> 8); mem[addr + 1] = (byte)val; },
            () => completed = true
        );

        var data = new byte[AdfDisk.TotalSize];
        // Put sync word + data in track
        data[0] = 0x44; data[1] = 0x89; // Sync
        data[2] = 0xDE; data[3] = 0xAD; // Data

        var drive = new FloppyDrive();
        drive.InsertDisk(new AdfDisk(data));
        drive.MotorOn = true;

        dma.DskPt = 0x1000;
        dma.WriteDskLen(0x8001);
        dma.WriteDskLen(0x8001); // Start: read 1 word

        // Cycle until sync found and transfer complete
        for (int i = 0; i < 100 && dma.Active; i++)
            dma.ExecuteCycle(drive);

        Assert.True(completed);
    }
}
