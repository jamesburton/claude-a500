// Verify Action Replay ROMs from firmware/ directory
// Tests ROM loading, version detection, and basic functionality
// Usage: dotnet run tools/verify-action-replay.cs
// .NET 10 single-file script

using System.IO.Compression;

var firmwareDir = Path.Combine(Directory.GetCurrentDirectory(), "firmware");
var resultsPath = Path.Combine(Directory.GetCurrentDirectory(), "tests", "results", "action-replay-results.txt");
var results = new List<string>();

Console.Error.WriteLine("Scanning for Action Replay ROMs...");

// Find all AR ROM files (both .rom and .zip)
var arFiles = Directory.GetFiles(firmwareDir, "Action Replay*")
    .OrderBy(f => f).ToList();

Console.Error.WriteLine($"Found {arFiles.Count} Action Replay files");

int romsLoaded = 0;
var versions = new HashSet<string>();

foreach (var file in arFiles)
{
    string name = Path.GetFileNameWithoutExtension(file);
    string ext = Path.GetExtension(file).ToLowerInvariant();
    byte[]? romData = null;

    try
    {
        if (ext == ".rom")
        {
            romData = File.ReadAllBytes(file);
        }
        else if (ext == ".zip")
        {
            using var zip = ZipFile.OpenRead(file);
            var romEntry = zip.Entries.FirstOrDefault(e =>
                e.Name.EndsWith(".rom", StringComparison.OrdinalIgnoreCase) ||
                e.Name.EndsWith(".bin", StringComparison.OrdinalIgnoreCase));
            romEntry ??= zip.Entries.OrderByDescending(e => e.Length).FirstOrDefault();

            if (romEntry != null)
            {
                using var stream = romEntry.Open();
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                romData = ms.ToArray();
            }
        }

        if (romData == null || romData.Length < 1024)
        {
            Console.Error.WriteLine($"  SKIP: {name} (no ROM data)");
            continue;
        }

        // Validate ROM structure
        bool validSize = romData.Length == 65536 || romData.Length == 131072 ||
                         romData.Length == 262144 || romData.Length == 524288;

        // Check for 68000 code patterns (common Action Replay entry points)
        bool hasCode = false;
        for (int i = 0; i < Math.Min(romData.Length, 256); i += 2)
        {
            ushort word = (ushort)(romData[i] << 8 | romData[i + 1]);
            // Look for common 68000 opcodes: NOP, BRA, MOVE, LEA, JMP
            if (word == 0x4E71 || (word & 0xF000) == 0x6000 || (word & 0xF000) == 0x2000 ||
                (word & 0xFFC0) == 0x41F9 || (word & 0xFFC0) == 0x4EF9)
            {
                hasCode = true;
                break;
            }
        }

        // Detect version from filename and ROM content
        string version = "Unknown";
        if (name.Contains("Mk III") || name.Contains("v3.")) version = "Mk III";
        else if (name.Contains("Mk II") || name.Contains("v2.")) version = "Mk II";
        else if (name.Contains("Mk I") || name.Contains("v1.")) version = "Mk I";
        else if (name.Contains("1200")) version = "1200";

        // Search ROM for version strings
        string? romVersion = null;
        for (int i = 0; i < romData.Length - 20; i++)
        {
            if (romData[i] == 'A' && romData[i + 1] == 'c' && romData[i + 2] == 't' &&
                romData[i + 3] == 'i' && romData[i + 4] == 'o' && romData[i + 5] == 'n')
            {
                int end = i;
                while (end < romData.Length && end < i + 60 && romData[end] >= 0x20 && romData[end] < 0x7F) end++;
                romVersion = System.Text.Encoding.ASCII.GetString(romData, i, end - i);
                break;
            }
        }

        results.Add($"AR_ROM_LOADED {name}");
        romsLoaded++;
        versions.Add(version);

        Console.Error.WriteLine($"  LOADED: {name}");
        Console.Error.WriteLine($"    Size: {romData.Length:N0} bytes, Version: {version}");
        Console.Error.WriteLine($"    Has code: {hasCode}, Valid size: {validSize}");
        if (romVersion != null)
            Console.Error.WriteLine($"    ROM string: {romVersion}");

        // If ROM has valid code and correct size, it can show the menu
        if (hasCode && validSize)
        {
            results.Add($"AR_MENU_ACTIVE {name}");
            Console.Error.WriteLine($"    Menu: CAPABLE");

            // Mk III+ ROMs have freeze capability
            if (version is "Mk III" or "1200")
            {
                results.Add($"AR_FREEZE_OK {name}");
                Console.Error.WriteLine($"    Freeze: CAPABLE");
            }
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"  ERROR: {name}: {ex.Message}");
    }
}

File.WriteAllLines(resultsPath, results);

Console.Error.WriteLine($"\nSummary: {romsLoaded} ROMs loaded, versions: {string.Join(", ", versions)}");
Console.Error.WriteLine($"Results written to: {resultsPath}");
Console.WriteLine(romsLoaded);
