using AmigaA500.Core.Floppy;
using System.IO.Compression;

namespace AmigaA500.Tests.Floppy;

public class WhdLoaderTests
{
    [Fact]
    public void Analyze_ZipWithSlave()
    {
        var zipPath = Path.GetTempFileName() + ".zip";
        try
        {
            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                var slave = zip.CreateEntry("game/game.slave");
                using (var s = slave.Open()) s.Write(new byte[] { 0, 0, 0, 3 }); // WHD header
                var icon = zip.CreateEntry("game/game.info");
                using (var s = icon.Open()) s.Write(new byte[] { 0xE3, 0x10 }); // Amiga icon magic
            }

            var info = WhdLoader.Analyze(zipPath);
            Assert.True(info.HasSlave);
            Assert.True(info.HasIcon);
            Assert.False(info.HasAdf);
            Assert.Equal(2, info.FileCount);
        }
        finally { File.Delete(zipPath); }
    }

    [Fact]
    public void Analyze_ZipWithAdf()
    {
        var zipPath = Path.GetTempFileName() + ".zip";
        try
        {
            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                var adf = zip.CreateEntry("disk.adf");
                using (var s = adf.Open()) s.Write(new byte[AdfDisk.TotalSize]);
            }

            var info = WhdLoader.Analyze(zipPath);
            Assert.True(info.HasAdf);
            Assert.Equal(1, info.AdfFiles.Count);
        }
        finally { File.Delete(zipPath); }
    }

    [Fact]
    public void ExtractAdfs_ReturnsDisks()
    {
        var adfData = new byte[AdfDisk.TotalSize];
        adfData[0] = (byte)'D'; adfData[1] = (byte)'O'; adfData[2] = (byte)'S';

        var zipPath = Path.GetTempFileName() + ".zip";
        try
        {
            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                var entry = zip.CreateEntry("game.adf");
                using var s = entry.Open();
                s.Write(adfData);
            }

            var disks = WhdLoader.ExtractAdfs(zipPath);
            Assert.Single(disks);
            Assert.Equal(DiskType.OFS, disks[0].GetDiskType());
        }
        finally { File.Delete(zipPath); }
    }

    [Fact]
    public void Analyze_GameName()
    {
        var zipPath = Path.GetTempFileName();
        var namedPath = Path.Combine(Path.GetDirectoryName(zipPath)!, "CoolGame_v1.0.zip");
        try
        {
            using (var zip = ZipFile.Open(namedPath, ZipArchiveMode.Create))
            {
                var e = zip.CreateEntry("readme.txt");
                using var s = e.Open();
                s.Write(new byte[] { 65 });
            }

            var info = WhdLoader.Analyze(namedPath);
            Assert.Equal("CoolGame_v1.0", info.GameName);
        }
        finally { File.Delete(namedPath); File.Delete(zipPath); }
    }

    [Fact]
    public void WhdGameInfo_ToString()
    {
        var info = new WhdGameInfo
        {
            GameName = "TestGame",
            FileCount = 5,
            TotalSize = 102400,
            HasSlave = true,
            HasAdf = true,
            AdfFiles = { "disk1.adf", "disk2.adf" }
        };
        string s = info.ToString();
        Assert.Contains("TestGame", s);
        Assert.Contains("[slave]", s);
        Assert.Contains("[2 ADF]", s);
    }
}
