// Verify real ADF files from firmware/pd-adfs/ directory
// Extracts ZIPs, validates ADF structure, and tests bootability
// Appends REAL_BOOT/REAL_PASS/FAIL results to adf-verify-results.txt
// Usage: dotnet run tools/verify-real-adfs.cs [--max N]
// .NET 10 single-file script

using System.IO.Compression;

int maxVerify = 200;
for (int i = 0; i < args.Length; i++)
    if (args[i] == "--max" && i + 1 < args.Length) maxVerify = int.Parse(args[++i]);

string pdDir = Path.Combine(Directory.GetCurrentDirectory(), "firmware", "pd-adfs");
string resultsPath = Path.Combine(Directory.GetCurrentDirectory(), "tests", "results", "adf-verify-results.txt");

if (!Directory.Exists(pdDir))
{
    Console.Error.WriteLine($"No pd-adfs directory found at {pdDir}");
    Console.WriteLine(0);
    return;
}

// Read existing results to avoid duplicates
var existingResults = new HashSet<string>();
if (File.Exists(resultsPath))
{
    foreach (var line in File.ReadAllLines(resultsPath))
    {
        if (line.StartsWith("REAL_"))
        {
            var name = line.Substring(line.IndexOf(' ') + 1);
            existingResults.Add(name);
        }
    }
}

var zipFiles = Directory.GetFiles(pdDir, "*.zip")
    .OrderBy(f => f)
    .Take(maxVerify)
    .ToList();

Console.Error.WriteLine($"Found {zipFiles.Count} ZIP files in {pdDir}");

int realBooted = 0, realPassed = 0, failed = 0, skipped = 0;
var newResults = new List<string>();

foreach (var zipPath in zipFiles)
{
    string baseName = Path.GetFileNameWithoutExtension(zipPath);

    if (existingResults.Contains(baseName))
    {
        skipped++;
        continue;
    }

    try
    {
        // Extract ADF from ZIP
        byte[]? adfData = null;
        using (var zip = ZipFile.OpenRead(zipPath))
        {
            var adfEntry = zip.Entries.FirstOrDefault(e =>
                e.Name.EndsWith(".adf", StringComparison.OrdinalIgnoreCase));

            if (adfEntry == null)
            {
                // Some ZIPs contain the ADF with same name
                adfEntry = zip.Entries.FirstOrDefault(e => e.Length == 901120);
            }

            if (adfEntry != null)
            {
                using var stream = adfEntry.Open();
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                adfData = ms.ToArray();
            }
        }

        if (adfData == null || adfData.Length != 901120)
        {
            newResults.Add($"FAIL {baseName} (no valid ADF in ZIP, got {adfData?.Length ?? 0} bytes)");
            failed++;
            continue;
        }

        // Check DOS header
        bool hasDosHeader = adfData[0] == 'D' && adfData[1] == 'O' && adfData[2] == 'S';

        // Check bootblock checksum
        bool validChecksum = false;
        if (hasDosHeader)
        {
            uint sum = 0;
            for (int i = 0; i < 1024; i += 4)
            {
                uint word = (uint)(adfData[i] << 24 | adfData[i + 1] << 16 | adfData[i + 2] << 8 | adfData[i + 3]);
                uint prev = sum;
                sum += word;
                if (sum < prev) sum++;
            }
            validChecksum = sum == 0xFFFFFFFF;
        }

        // Check for non-empty content beyond bootblock
        bool hasContentBeyondBoot = false;
        for (int i = 1024; i < Math.Min(adfData.Length, 10240); i++)
        {
            if (adfData[i] != 0) { hasContentBeyondBoot = true; break; }
        }

        // Check bootblock itself has code (bytes 12+ in bootblock)
        bool hasBootCode = false;
        for (int i = 12; i < 1024; i++)
        {
            if (adfData[i] != 0) { hasBootCode = true; break; }
        }

        // Determine disk type
        string diskType = adfData[3] switch
        {
            0 => "OFS",
            1 => "FFS",
            2 => "OFS-Intl",
            3 => "FFS-Intl",
            _ => "Unknown"
        };

        // Classification: DOS header + valid checksum = bootable (even boot-only demos)
        // DOS header + no checksum but has content = loadable
        if (hasDosHeader && validChecksum)
        {
            newResults.Add($"REAL_BOOT {baseName}");
            realBooted++;
            Console.Error.WriteLine($"  REAL_BOOT: {baseName} ({diskType}{(hasContentBeyondBoot ? "" : ", boot-only")})");
        }
        else if (hasDosHeader && (hasContentBeyondBoot || hasBootCode))
        {
            newResults.Add($"REAL_PASS {baseName}");
            realPassed++;
            Console.Error.WriteLine($"  REAL_PASS: {baseName} ({diskType}, checksum invalid)");
        }
        else
        {
            newResults.Add($"FAIL {baseName} (DOS={hasDosHeader} Checksum={validChecksum} Content={hasContentBeyondBoot} Boot={hasBootCode})");
            failed++;
        }
    }
    catch (Exception ex)
    {
        newResults.Add($"FAIL {baseName} ({ex.Message})");
        failed++;
    }
}

// Append new results
if (newResults.Count > 0)
    File.AppendAllLines(resultsPath, newResults);

Console.Error.WriteLine($"\nResults: {realBooted} REAL_BOOT, {realPassed} REAL_PASS, {failed} FAIL, {skipped} skipped");
Console.WriteLine(realBooted + realPassed);
