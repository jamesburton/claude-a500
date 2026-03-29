using System.IO.Compression;

namespace AmigaA500.Core.Floppy;

/// <summary>
/// Loads ADF disk images from various archive formats (ZIP, LHA).
/// Supports .adf, .zip (containing .adf), and .lha (containing .adf).
/// </summary>
public static class ArchiveLoader
{
    /// <summary>
    /// Load an ADF from a file path. Automatically handles .adf, .zip, and .lha.
    /// </summary>
    public static AdfDisk LoadFromFile(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".adf" => AdfDisk.Load(path),
            ".zip" => LoadFromZip(path),
            ".lha" or ".lzh" => LoadFromLha(path),
            _ => throw new NotSupportedException($"Unsupported format: {ext}")
        };
    }

    /// <summary>
    /// Load an ADF from a ZIP archive. Finds the first .adf entry or 901120-byte entry.
    /// </summary>
    public static AdfDisk LoadFromZip(string zipPath)
    {
        using var zip = ZipFile.OpenRead(zipPath);

        // Try to find an .adf file first
        var adfEntry = zip.Entries.FirstOrDefault(e =>
            e.Name.EndsWith(".adf", StringComparison.OrdinalIgnoreCase));

        // Fallback: find any entry that's exactly ADF size
        adfEntry ??= zip.Entries.FirstOrDefault(e => e.Length == AdfDisk.TotalSize);

        if (adfEntry == null)
            throw new FileNotFoundException($"No ADF found in ZIP: {zipPath}");

        using var stream = adfEntry.Open();
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var data = ms.ToArray();

        if (data.Length != AdfDisk.TotalSize)
            throw new InvalidDataException($"ADF in ZIP is {data.Length} bytes, expected {AdfDisk.TotalSize}");

        return new AdfDisk(data);
    }

    /// <summary>
    /// Load an ADF from an LHA/LZH archive.
    /// Uses a basic LHA header parser to find and extract the ADF entry.
    /// </summary>
    public static AdfDisk LoadFromLha(string lhaPath)
    {
        var data = File.ReadAllBytes(lhaPath);
        var extracted = LhaExtractor.ExtractFirstAdf(data);
        if (extracted == null)
            throw new FileNotFoundException($"No ADF found in LHA: {lhaPath}");

        return new AdfDisk(extracted);
    }

    /// <summary>
    /// Try loading from any supported format. Returns null on failure.
    /// </summary>
    public static AdfDisk? TryLoad(string path)
    {
        try { return LoadFromFile(path); }
        catch { return null; }
    }

    /// <summary>
    /// Check if a file is a supported archive format.
    /// </summary>
    public static bool IsSupportedArchive(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        return ext is ".adf" or ".zip" or ".lha" or ".lzh";
    }

    /// <summary>
    /// List ADF entries found in a ZIP archive.
    /// </summary>
    public static List<string> ListAdfsInZip(string zipPath)
    {
        using var zip = ZipFile.OpenRead(zipPath);
        return zip.Entries
            .Where(e => e.Name.EndsWith(".adf", StringComparison.OrdinalIgnoreCase) || e.Length == AdfDisk.TotalSize)
            .Select(e => e.FullName)
            .ToList();
    }

    /// <summary>
    /// Load a specific ADF entry from a ZIP by name.
    /// </summary>
    public static AdfDisk LoadFromZipEntry(string zipPath, string entryName)
    {
        using var zip = ZipFile.OpenRead(zipPath);
        var entry = zip.GetEntry(entryName)
            ?? throw new FileNotFoundException($"Entry '{entryName}' not found in {zipPath}");

        using var stream = entry.Open();
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return new AdfDisk(ms.ToArray());
    }
}
