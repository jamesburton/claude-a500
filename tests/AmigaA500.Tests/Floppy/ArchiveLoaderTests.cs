using AmigaA500.Core.Floppy;
using System.IO.Compression;

namespace AmigaA500.Tests.Floppy;

public class ArchiveLoaderTests
{
    [Fact]
    public void IsSupportedArchive_Recognizes_All_Formats()
    {
        Assert.True(ArchiveLoader.IsSupportedArchive("game.adf"));
        Assert.True(ArchiveLoader.IsSupportedArchive("game.zip"));
        Assert.True(ArchiveLoader.IsSupportedArchive("game.lha"));
        Assert.True(ArchiveLoader.IsSupportedArchive("game.lzh"));
        Assert.False(ArchiveLoader.IsSupportedArchive("game.exe"));
        Assert.False(ArchiveLoader.IsSupportedArchive("game.txt"));
    }

    [Fact]
    public void LoadFromZip_ExtractsAdf()
    {
        // Create a temp ZIP with an ADF inside
        var adfData = new byte[AdfDisk.TotalSize];
        adfData[0] = (byte)'D'; adfData[1] = (byte)'O'; adfData[2] = (byte)'S'; adfData[3] = 0;

        var zipPath = Path.GetTempFileName() + ".zip";
        try
        {
            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                var entry = zip.CreateEntry("test.adf");
                using var stream = entry.Open();
                stream.Write(adfData);
            }

            var disk = ArchiveLoader.LoadFromZip(zipPath);
            Assert.Equal(DiskType.OFS, disk.GetDiskType());
        }
        finally
        {
            File.Delete(zipPath);
        }
    }

    [Fact]
    public void LoadFromZip_FindsBySize()
    {
        var adfData = new byte[AdfDisk.TotalSize];

        var zipPath = Path.GetTempFileName() + ".zip";
        try
        {
            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                var entry = zip.CreateEntry("disk.bin"); // Not .adf extension
                using var stream = entry.Open();
                stream.Write(adfData);
            }

            var disk = ArchiveLoader.LoadFromZip(zipPath);
            Assert.NotNull(disk);
        }
        finally
        {
            File.Delete(zipPath);
        }
    }

    [Fact]
    public void LoadFromZip_ThrowsIfNoAdf()
    {
        var zipPath = Path.GetTempFileName() + ".zip";
        try
        {
            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                var entry = zip.CreateEntry("readme.txt");
                using var stream = entry.Open();
                stream.Write(new byte[] { 65, 66, 67 });
            }

            Assert.Throws<FileNotFoundException>(() => ArchiveLoader.LoadFromZip(zipPath));
        }
        finally
        {
            File.Delete(zipPath);
        }
    }

    [Fact]
    public void TryLoad_ReturnsNullOnFailure()
    {
        Assert.Null(ArchiveLoader.TryLoad("/nonexistent/path.adf"));
    }

    [Fact]
    public void ListAdfsInZip_ReturnsEntries()
    {
        var adfData = new byte[AdfDisk.TotalSize];
        var zipPath = Path.GetTempFileName() + ".zip";
        try
        {
            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                var e1 = zip.CreateEntry("disk1.adf");
                using (var s = e1.Open()) s.Write(adfData);
                var e2 = zip.CreateEntry("disk2.adf");
                using (var s = e2.Open()) s.Write(adfData);
            }

            var list = ArchiveLoader.ListAdfsInZip(zipPath);
            Assert.Equal(2, list.Count);
        }
        finally
        {
            File.Delete(zipPath);
        }
    }

    [Fact]
    public void LoadFromZipEntry_ByName()
    {
        var adfData = new byte[AdfDisk.TotalSize];
        adfData[0] = (byte)'D'; adfData[1] = (byte)'O'; adfData[2] = (byte)'S'; adfData[3] = 1; // FFS
        var zipPath = Path.GetTempFileName() + ".zip";
        try
        {
            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                var entry = zip.CreateEntry("mygame.adf");
                using var stream = entry.Open();
                stream.Write(adfData);
            }

            var disk = ArchiveLoader.LoadFromZipEntry(zipPath, "mygame.adf");
            Assert.Equal(DiskType.FFS, disk.GetDiskType());
        }
        finally
        {
            File.Delete(zipPath);
        }
    }
}
