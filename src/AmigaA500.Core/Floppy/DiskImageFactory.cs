using System.IO.Compression;

namespace AmigaA500.Core.Floppy;

/// <summary>
/// Universal disk image factory — loads disk images from any supported format.
/// Handles .adf, .zip (containing .adf), .lha (containing .adf), .hdf, and nested archives.
/// </summary>
public static class DiskImageFactory
{
    /// <summary>
    /// Load all disk images from a path. Supports single files, directories, and archives.
    /// Returns a list of (name, disk) tuples.
    /// </summary>
    public static List<(string name, AdfDisk disk)> LoadAll(string path)
    {
        var results = new List<(string, AdfDisk)>();

        if (Directory.Exists(path))
        {
            foreach (var file in Directory.GetFiles(path))
            {
                if (!ArchiveLoader.IsSupportedArchive(file)) continue;
                var disk = ArchiveLoader.TryLoad(file);
                if (disk != null)
                    results.Add((Path.GetFileNameWithoutExtension(file), disk));
            }
        }
        else if (File.Exists(path))
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".zip")
            {
                // Check for nested ZIPs (multi-game archives)
                try
                {
                    using var zip = ZipFile.OpenRead(path);
                    foreach (var entry in zip.Entries)
                    {
                        if (entry.Name.EndsWith(".adf", StringComparison.OrdinalIgnoreCase))
                        {
                            using var stream = entry.Open();
                            using var ms = new MemoryStream();
                            stream.CopyTo(ms);
                            var data = ms.ToArray();
                            if (data.Length == AdfDisk.TotalSize)
                                results.Add((Path.GetFileNameWithoutExtension(entry.Name), new AdfDisk(data)));
                        }
                        else if (entry.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            // Nested ZIP
                            using var stream = entry.Open();
                            using var ms = new MemoryStream();
                            stream.CopyTo(ms);
                            ms.Position = 0;
                            try
                            {
                                using var inner = new ZipArchive(ms, ZipArchiveMode.Read);
                                foreach (var innerEntry in inner.Entries)
                                {
                                    if (innerEntry.Length != AdfDisk.TotalSize &&
                                        !innerEntry.Name.EndsWith(".adf", StringComparison.OrdinalIgnoreCase))
                                        continue;
                                    using var iStream = innerEntry.Open();
                                    using var iMs = new MemoryStream();
                                    iStream.CopyTo(iMs);
                                    var iData = iMs.ToArray();
                                    if (iData.Length == AdfDisk.TotalSize)
                                        results.Add((Path.GetFileNameWithoutExtension(innerEntry.Name), new AdfDisk(iData)));
                                }
                            }
                            catch { }
                        }
                    }
                }
                catch { }

                // If no entries found via traversal, try direct load
                if (results.Count == 0)
                {
                    var disk = ArchiveLoader.TryLoad(path);
                    if (disk != null)
                        results.Add((Path.GetFileNameWithoutExtension(path), disk));
                }
            }
            else
            {
                var disk = ArchiveLoader.TryLoad(path);
                if (disk != null)
                    results.Add((Path.GetFileNameWithoutExtension(path), disk));
            }
        }

        return results;
    }

    /// <summary>
    /// Scan a directory recursively for all disk images.
    /// </summary>
    public static List<string> ScanDirectory(string path)
    {
        var supported = new[] { ".adf", ".zip", ".lha", ".lzh" };
        return Directory.GetFiles(path, "*", SearchOption.AllDirectories)
            .Where(f => supported.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .ToList();
    }
}
