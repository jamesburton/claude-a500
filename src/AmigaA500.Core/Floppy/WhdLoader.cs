using System.IO.Compression;

namespace AmigaA500.Core.Floppy;

/// <summary>
/// WHDLoad game package loader — extracts and manages WHD game installations.
/// WHDLoad games use .slave files as entry points and may contain disk images or raw data.
/// </summary>
public sealed class WhdLoader
{
    /// <summary>
    /// Scan a WHD archive for game content.
    /// </summary>
    public static WhdGameInfo Analyze(string archivePath)
    {
        var info = new WhdGameInfo { ArchivePath = archivePath };
        string ext = Path.GetExtension(archivePath).ToLowerInvariant();

        if (ext == ".zip")
            AnalyzeZip(archivePath, info);
        else if (ext is ".lha" or ".lzh")
            AnalyzeLha(archivePath, info);

        return info;
    }

    private static void AnalyzeZip(string path, WhdGameInfo info)
    {
        try
        {
            using var zip = ZipFile.OpenRead(path);
            foreach (var entry in zip.Entries)
            {
                string name = entry.Name.ToLowerInvariant();
                if (name.EndsWith(".slave")) info.HasSlave = true;
                if (name.EndsWith(".adf")) { info.HasAdf = true; info.AdfFiles.Add(entry.FullName); }
                if (name.EndsWith(".info")) info.HasIcon = true;
                if (name.EndsWith(".readme") || name.EndsWith(".txt")) info.HasReadme = true;
                info.TotalSize += entry.Length;
                info.FileCount++;
            }
            info.GameName = Path.GetFileNameWithoutExtension(path);
        }
        catch { }
    }

    private static void AnalyzeLha(string path, WhdGameInfo info)
    {
        try
        {
            var data = File.ReadAllBytes(path);
            info.TotalSize = data.Length;
            info.GameName = Path.GetFileNameWithoutExtension(path);

            // Check LHA header for entry names
            var entries = LhaExtractor.ListEntries(data);
            foreach (var entry in entries)
            {
                string name = entry.Filename.ToLowerInvariant();
                if (name.EndsWith(".slave")) info.HasSlave = true;
                if (name.EndsWith(".adf")) { info.HasAdf = true; info.AdfFiles.Add(entry.Filename); }
                if (name.EndsWith(".info")) info.HasIcon = true;
                info.FileCount++;
            }
        }
        catch { }
    }

    /// <summary>
    /// Extract ADF files from a WHD archive.
    /// </summary>
    public static List<AdfDisk> ExtractAdfs(string archivePath)
    {
        var disks = new List<AdfDisk>();
        string ext = Path.GetExtension(archivePath).ToLowerInvariant();

        if (ext == ".zip")
        {
            try
            {
                using var zip = ZipFile.OpenRead(archivePath);
                foreach (var entry in zip.Entries)
                {
                    if (!entry.Name.EndsWith(".adf", StringComparison.OrdinalIgnoreCase)) continue;
                    using var stream = entry.Open();
                    using var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    var data = ms.ToArray();
                    if (data.Length == AdfDisk.TotalSize)
                        disks.Add(new AdfDisk(data));
                }
            }
            catch { }
        }

        return disks;
    }
}

public class WhdGameInfo
{
    public string ArchivePath = "";
    public string GameName = "";
    public bool HasSlave;
    public bool HasAdf;
    public bool HasIcon;
    public bool HasReadme;
    public List<string> AdfFiles = new();
    public long TotalSize;
    public int FileCount;

    public override string ToString() =>
        $"{GameName}: {FileCount} files, {TotalSize / 1024}KB" +
        $"{(HasSlave ? " [slave]" : "")}{(HasAdf ? $" [{AdfFiles.Count} ADF]" : "")}" +
        $"{(HasIcon ? " [icon]" : "")}";
}
