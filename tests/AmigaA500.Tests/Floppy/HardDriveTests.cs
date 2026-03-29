using AmigaA500.Core.Floppy;

namespace AmigaA500.Tests.Floppy;

public class HardDriveTests
{
    [Fact]
    public void Create_CorrectSize()
    {
        var hdf = HardDrive.Create(10); // 10 MB
        Assert.Equal(10 * 1024 * 1024, hdf.TotalBytes);
        Assert.Equal(10 * 1024 * 1024 / 512, hdf.TotalSectors);
    }

    [Fact]
    public void ReadWrite_Sector()
    {
        var hdf = HardDrive.Create(1);
        var sector = new byte[512];
        sector[0] = 0xDE; sector[1] = 0xAD;
        hdf.WriteSector(0, sector);

        var read = hdf.ReadSector(0);
        Assert.Equal(0xDE, read[0]);
        Assert.Equal(0xAD, read[1]);
    }

    [Fact]
    public void ReadWord_BigEndian()
    {
        var hdf = HardDrive.Create(1);
        var sector = new byte[512];
        sector[0] = 0x12; sector[1] = 0x34;
        hdf.WriteSector(0, sector);

        Assert.Equal(0x1234, hdf.ReadWord(0, 0));
    }

    [Fact]
    public void ReadLong_BigEndian()
    {
        var hdf = HardDrive.Create(1);
        var sector = new byte[512];
        sector[0] = 0xDE; sector[1] = 0xAD; sector[2] = 0xBE; sector[3] = 0xEF;
        hdf.WriteSector(0, sector);

        Assert.Equal(0xDEADBEEFu, hdf.ReadLong(0, 0));
    }

    [Fact]
    public void HasRdb_FalseByDefault()
    {
        var hdf = HardDrive.Create(1);
        Assert.False(hdf.HasRdb());
    }

    [Fact]
    public void HasRdb_TrueWithRdskMagic()
    {
        var hdf = HardDrive.Create(1);
        var sector = new byte[512];
        sector[0] = 0x52; sector[1] = 0x44; sector[2] = 0x53; sector[3] = 0x4B; // "RDSK"
        hdf.WriteSector(0, sector);

        Assert.True(hdf.HasRdb());
    }

    [Fact]
    public void WriteSector_BadSize_Throws()
    {
        var hdf = HardDrive.Create(1);
        Assert.Throws<ArgumentException>(() => hdf.WriteSector(0, new byte[100]));
    }

    [Fact]
    public void ReadSector_OutOfRange_Throws()
    {
        var hdf = HardDrive.Create(1);
        Assert.Throws<ArgumentOutOfRangeException>(() => hdf.ReadSector(hdf.TotalSectors + 1));
    }

    [Fact]
    public void GetPartitions_EmptyWithoutRdb()
    {
        var hdf = HardDrive.Create(1);
        Assert.Empty(hdf.GetPartitions());
    }
}
