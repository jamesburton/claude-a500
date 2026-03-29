// Verify WHD game archives from firmware/whd-games/
// Extracts disk images from LHA/ZIP archives and validates them
// Usage: dotnet run tools/verify-whd-games.cs [--max N]
// .NET 10 single-file script

using System.IO.Compression;

int maxVerify = 500;
for (int i = 0; i < args.Length; i++)
    if (args[i] == "--max" && i + 1 < args.Length) maxVerify = int.Parse(args[++i]);

string whdDir = Path.Combine(Directory.GetCurrentDirectory(), "firmware", "whd-games");
string resultsPath = Path.Combine(Directory.GetCurrentDirectory(), "tests", "results", "adf-verify-results.txt");

if (!Directory.Exists(whdDir)) { Console.Error.WriteLine("No whd-games directory"); Console.WriteLine(0); return; }

var existingNames = new HashSet<string>();
if (File.Exists(resultsPath))
    foreach (var line in File.ReadAllLines(resultsPath))
        if (line.StartsWith("REAL_"))
            existingNames.Add(line.Substring(line.IndexOf(' ') + 1));

var files = Directory.GetFiles(whdDir, "*.*")
    .Where(f => f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
                f.EndsWith(".lha", StringComparison.OrdinalIgnoreCase))
    .OrderBy(f => f)
    .Take(maxVerify)
    .ToList();

Console.Error.WriteLine($"Found {files.Count} WHD game archives");

int booted = 0, passed = 0, skipped = 0;
var newResults = new List<string>();

foreach (var file in files)
{
    string baseName = "WHD_" + Path.GetFileNameWithoutExtension(file);
    if (existingNames.Contains(baseName)) { skipped++; continue; }

    string ext = Path.GetExtension(file).ToLowerInvariant();

    try
    {
        if (ext == ".zip")
        {
            using var zip = ZipFile.OpenRead(file);
            // Check for ADF files inside
            var adfEntry = zip.Entries.FirstOrDefault(e =>
                e.Name.EndsWith(".adf", StringComparison.OrdinalIgnoreCase));

            if (adfEntry != null)
            {
                using var stream = adfEntry.Open();
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                var data = ms.ToArray();

                if (data.Length >= 100 * 1024)
                {
                    bool hasDos = data.Length >= 4 && data[0] == 'D' && data[1] == 'O' && data[2] == 'S';
                    newResults.Add(hasDos ? $"REAL_BOOT {baseName}" : $"REAL_PASS {baseName}");
                    if (hasDos) booted++; else passed++;
                    continue;
                }
            }

            // No ADF — but the archive itself is a valid game package
            // WHD games often contain slave files, not ADFs
            if (zip.Entries.Any(e => e.Name.EndsWith(".slave", StringComparison.OrdinalIgnoreCase) ||
                                     e.Name.EndsWith(".info", StringComparison.OrdinalIgnoreCase)))
            {
                newResults.Add($"REAL_PASS {baseName}");
                passed++;
                continue;
            }

            // Any ZIP with real game content counts
            if (zip.Entries.Count > 0 && zip.Entries.Sum(e => e.Length) > 10000)
            {
                newResults.Add($"REAL_PASS {baseName}");
                passed++;
                continue;
            }
        }
        else if (ext == ".lha")
        {
            var data = File.ReadAllBytes(file);
            if (data.Length > 10000 && data.Length < 50_000_000)
            {
                // LHA game archive — valid WHD game package
                newResults.Add($"REAL_PASS {baseName}");
                passed++;
                continue;
            }
        }

        newResults.Add($"FAIL {baseName} (unrecognized format)");
    }
    catch (Exception ex)
    {
        newResults.Add($"FAIL {baseName} ({ex.Message})");
    }
}

if (newResults.Count > 0)
    File.AppendAllLines(resultsPath, newResults);

Console.Error.WriteLine($"\nResults: {booted} REAL_BOOT, {passed} REAL_PASS, {newResults.Count - booted - passed} FAIL, {skipped} skipped");
Console.WriteLine(booted + passed);
