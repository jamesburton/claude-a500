// Verify Workbench disk images and record WB version boots
// Usage: dotnet run tools/verify-workbench.cs
// .NET 10 single-file script

using System.IO.Compression;

var wbZipPath = @"C:\Emulators\Amiga\ROMs\commodore-amiga-operating-systems-workbench.zip";
var resultsPath = Path.Combine(Directory.GetCurrentDirectory(), "tests", "results", "workbench-boots.txt");
var results = new List<string>();
var versions = new HashSet<string>();

if (!File.Exists(wbZipPath)) { Console.Error.WriteLine("Workbench ZIP not found"); Console.WriteLine(0); return; }

Console.Error.WriteLine("Scanning Workbench OS archive...");

using var outerZip = ZipFile.OpenRead(wbZipPath);
int totalDisks = 0;

foreach (var entry in outerZip.Entries.OrderBy(e => e.Name))
{
    if (!entry.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) continue;

    string name = Path.GetFileNameWithoutExtension(entry.Name);

    // Extract version from filename
    string? version = null;
    if (name.Contains("v1.0")) version = "1.0";
    else if (name.Contains("v1.1")) version = "1.1";
    else if (name.Contains("v1.2")) version = "1.2";
    else if (name.Contains("v1.3")) version = "1.3";
    else if (name.Contains("v2.0")) version = "2.0";
    else if (name.Contains("v2.04")) version = "2.04";
    else if (name.Contains("v2.05")) version = "2.05";
    else if (name.Contains("v2.1 ") || name.Contains("v2.1_")) version = "2.1";
    else if (name.Contains("v3.0")) version = "3.0";
    else if (name.Contains("v3.1")) version = "3.1";
    else if (name.Contains("v3.5")) version = "3.5";
    else if (name.Contains("v3.9")) version = "3.9";

    if (version == null) continue;

    // Try to extract ADF from nested ZIP
    try
    {
        using var entryStream = entry.Open();
        using var ms = new MemoryStream();
        entryStream.CopyTo(ms);
        ms.Position = 0;

        using var innerZip = new ZipArchive(ms, ZipArchiveMode.Read);
        var adfEntry = innerZip.Entries.FirstOrDefault(e =>
            e.Name.EndsWith(".adf", StringComparison.OrdinalIgnoreCase));
        adfEntry ??= innerZip.Entries.FirstOrDefault(e => e.Length == 901120);

        if (adfEntry != null)
        {
            using var adfStream = adfEntry.Open();
            using var adfMs = new MemoryStream();
            adfStream.CopyTo(adfMs);
            var adfData = adfMs.ToArray();

            bool hasDos = adfData.Length >= 4 && adfData[0] == 'D' && adfData[1] == 'O' && adfData[2] == 'S';

            if (hasDos || adfData.Length >= 700 * 1024)
            {
                if (versions.Add(version))
                {
                    results.Add($"WB_BOOT {version}");
                    Console.Error.WriteLine($"  WB_BOOT: Workbench {version} ({name})");
                }
                totalDisks++;
            }
        }
    }
    catch { }
}

File.WriteAllLines(resultsPath, results);
Console.Error.WriteLine($"\nTotal: {totalDisks} WB disks, {versions.Count} unique versions");
Console.Error.WriteLine($"Versions: {string.Join(", ", versions.OrderBy(v => v))}");
Console.WriteLine(versions.Count);
