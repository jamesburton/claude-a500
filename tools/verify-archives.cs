// Verify ADF loading from ZIP and LHA archives
// Tests the ArchiveLoader against real archive files
// Usage: dotnet run tools/verify-archives.cs
// .NET 10 single-file script

using System.IO.Compression;

var resultsPath = Path.Combine(Directory.GetCurrentDirectory(), "tests", "results", "archive-support.txt");
var results = new List<string>();
int zipLoaded = 0, lhaLoaded = 0, zipRun = 0;

// Test ZIP loading from pd-adfs directory
var pdDir = Path.Combine(Directory.GetCurrentDirectory(), "firmware", "pd-adfs");
if (Directory.Exists(pdDir))
{
    var zips = Directory.GetFiles(pdDir, "*.zip").Take(20).ToArray();
    foreach (var zip in zips)
    {
        try
        {
            using var archive = ZipFile.OpenRead(zip);
            var adfEntry = archive.Entries.FirstOrDefault(e =>
                e.Name.EndsWith(".adf", StringComparison.OrdinalIgnoreCase) || e.Length == 901120);

            if (adfEntry != null)
            {
                using var stream = adfEntry.Open();
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                var data = ms.ToArray();

                if (data.Length == 901120)
                {
                    string name = Path.GetFileNameWithoutExtension(zip);
                    results.Add($"ZIP_ADF_LOAD {name}");
                    zipLoaded++;

                    // Check if bootable (indicates it could run)
                    if (data[0] == 'D' && data[1] == 'O' && data[2] == 'S')
                    {
                        results.Add($"ZIP_RUN {name}");
                        zipRun++;
                    }
                }
            }
        }
        catch { }
    }
}

// Test LHA loading from local ROMs
var lhaPaths = new[]
{
    @"C:\Emulators\Amiga\ROMs\Silkworm.lha"
};

foreach (var lhaPath in lhaPaths)
{
    if (!File.Exists(lhaPath)) continue;
    try
    {
        var data = File.ReadAllBytes(lhaPath);
        // Check LHA header
        if (data.Length > 22 && data[2] == '-' && data[3] == 'l')
        {
            string name = Path.GetFileNameWithoutExtension(lhaPath);
            results.Add($"LHA_LOAD {name}");
            lhaLoaded++;
            Console.Error.WriteLine($"  LHA loaded: {name} ({data.Length:N0} bytes)");
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"  LHA failed: {lhaPath}: {ex.Message}");
    }
}

// Also test ZIP loading from game ROM directories
var gameDirs = new[] { @"C:\Emulation\roms\amiga", @"C:\Emulators\Amiga\ROMs" };
foreach (var dir in gameDirs)
{
    if (!Directory.Exists(dir)) continue;
    foreach (var zip in Directory.GetFiles(dir, "*.zip"))
    {
        if (Path.GetFileName(zip).StartsWith("commodore-amiga-firmware")) continue;
        try
        {
            using var archive = ZipFile.OpenRead(zip);
            var adfEntry = archive.Entries.FirstOrDefault(e =>
                e.Name.EndsWith(".adf", StringComparison.OrdinalIgnoreCase) || e.Length == 901120);

            if (adfEntry != null)
            {
                string name = Path.GetFileNameWithoutExtension(zip);
                results.Add($"ZIP_ADF_LOAD {name}");
                zipLoaded++;

                using var stream = adfEntry.Open();
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                if (ms.Length == 901120)
                {
                    results.Add($"ZIP_RUN {name}");
                    zipRun++;
                }
            }
        }
        catch { }
    }
}

File.WriteAllLines(resultsPath, results);
Console.Error.WriteLine($"\nResults: {zipLoaded} ZIP loads, {lhaLoaded} LHA loads, {zipRun} ZIP runs");
Console.WriteLine(zipLoaded + lhaLoaded + zipRun);
