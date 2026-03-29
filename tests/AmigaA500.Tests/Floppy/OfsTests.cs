using AmigaA500.Core.Floppy;

namespace AmigaA500.Tests.Floppy;

public class OfsTests
{
    private static AdfDisk CreateDiskWithRootBlock(string volumeName)
    {
        var data = new byte[AdfDisk.TotalSize];
        // DOS header
        data[0] = (byte)'D'; data[1] = (byte)'O'; data[2] = (byte)'S'; data[3] = 0;

        // Root block at sector 880 (offset 880 * 512 = 450560)
        int rootOffset = 880 * 512;
        // Type = 2 (T_HEADER)
        data[rootOffset] = 0; data[rootOffset + 1] = 0; data[rootOffset + 2] = 0; data[rootOffset + 3] = 2;
        // Secondary type at offset 508 = 1 (ST_ROOT)
        data[rootOffset + 508] = 0; data[rootOffset + 509] = 0; data[rootOffset + 510] = 0; data[rootOffset + 511] = 1;
        // Volume name at offset 432
        data[rootOffset + 432] = (byte)volumeName.Length;
        for (int i = 0; i < volumeName.Length; i++)
            data[rootOffset + 433 + i] = (byte)volumeName[i];

        return new AdfDisk(data);
    }

    [Fact]
    public void GetVolumeName_ReadsCorrectly()
    {
        var disk = CreateDiskWithRootBlock("TestDisk");
        var ofs = new OfsFileSystem(disk);
        Assert.Equal("TestDisk", ofs.GetVolumeName());
    }

    [Fact]
    public void GetRootBlockType_IsHeader()
    {
        var disk = CreateDiskWithRootBlock("Test");
        var ofs = new OfsFileSystem(disk);
        Assert.Equal(2, ofs.GetRootBlockType()); // T_HEADER
    }

    [Fact]
    public void ListRootDirectory_EmptyDisk_ReturnsEmpty()
    {
        var disk = CreateDiskWithRootBlock("Empty");
        var ofs = new OfsFileSystem(disk);
        var entries = ofs.ListRootDirectory();
        Assert.Empty(entries);
    }

    [Fact]
    public void ReadBlock_ReturnsCorrectSector()
    {
        var data = new byte[AdfDisk.TotalSize];
        data[0] = 0xCA; data[1] = 0xFE;
        var disk = new AdfDisk(data);
        var ofs = new OfsFileSystem(disk);
        var block = ofs.ReadBlock(0);
        Assert.Equal(0xCA, block[0]);
        Assert.Equal(0xFE, block[1]);
    }

    [Fact]
    public void DirectoryEntry_ToString_File()
    {
        var entry = new DirectoryEntry { Name = "readme.txt", IsFile = true, Size = 1234 };
        Assert.Equal("readme.txt (1234 bytes)", entry.ToString());
    }

    [Fact]
    public void DirectoryEntry_ToString_Directory()
    {
        var entry = new DirectoryEntry { Name = "src", IsDirectory = true };
        Assert.Equal("[src]/", entry.ToString());
    }
}
