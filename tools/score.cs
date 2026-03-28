// Autoresearch scoring script for Commodore A500 emulator project
// Outputs a single integer: the composite project score
// Run: dotnet run tools/score.cs

using System.Text.RegularExpressions;

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, ".."));
// If run from project root, use current directory
if (!Directory.Exists(Path.Combine(root, "tools")))
    root = Directory.GetCurrentDirectory();

int score = 0;
var details = new List<string>();

void Award(int pts, string reason)
{
    score += pts;
    details.Add($"  {(pts >= 0 ? "+" : "")}{pts}: {reason}");
}

// ============================================================
// PHASE 1: RESEARCH (up to 250 points)
// ============================================================

// --- Architecture document (100 pts) ---
var archDoc = FindDoc(root, "architecture", "arch");
if (archDoc != null)
{
    var content = File.ReadAllText(archDoc).ToLowerInvariant();
    var subsystems = new[] { "68000", "cpu", "agnus", "denise", "paula", "cia", "kickstart", "rom", "memory", "bus", "dma", "blitter", "copper", "floppy", "audio", "video", "chipset" };
    int found = subsystems.Count(s => content.Contains(s));
    int archPts = Math.Min(100, (int)(100.0 * found / 12)); // need ~12 key terms for full marks
    Award(archPts, $"Architecture doc ({found}/{subsystems.Length} subsystems mentioned)");
}

// --- Sizing estimates (50 pts) ---
var sizingDoc = FindDoc(root, "sizing", "estimates", "complexity");
if (sizingDoc != null)
{
    var content = File.ReadAllText(sizingDoc).ToLowerInvariant();
    // Look for numeric estimates (LOC, hours, complexity ratings)
    var numbers = Regex.Matches(content, @"\b\d+\s*(loc|lines|hours|days|classes|files|kb|mb)\b");
    int sizingPts = Math.Min(50, numbers.Count * 5);
    Award(sizingPts, $"Sizing estimates ({numbers.Count} estimates found)");
}

// --- Technical analysis/specs (100 pts) ---
var specFiles = FindAllDocs(root, "spec", "technical", "implementation", "design");
if (specFiles.Count > 0)
{
    // Score by number of spec documents and their depth
    int totalSections = 0;
    foreach (var f in specFiles)
    {
        var lines = File.ReadAllLines(f);
        totalSections += lines.Count(l => l.StartsWith("#"));
    }
    int specPts = Math.Min(100, specFiles.Count * 10 + totalSections);
    Award(specPts, $"Technical specs ({specFiles.Count} docs, {totalSections} sections)");
}

// ============================================================
// PHASE 2: PLANNING (up to 50 points)
// ============================================================

var planFiles = FindAllDocs(root, "plan", "task", "roadmap", "milestone");
if (planFiles.Count > 0)
{
    int taskCount = 0;
    int subTaskCount = 0;
    foreach (var f in planFiles)
    {
        var lines = File.ReadAllLines(f);
        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("- [ ]") || trimmed.StartsWith("- [x]") || trimmed.StartsWith("- [X]")
                || Regex.IsMatch(trimmed, @"^#{1,3}\s+(task|step|phase)", RegexOptions.IgnoreCase))
            {
                if (line.StartsWith("  ") || line.StartsWith("\t"))
                    subTaskCount++;
                else
                    taskCount++;
            }
        }
    }
    // 5% for top-level breakdown, 95% distributed among sub-tasks
    int topPts = taskCount > 0 ? (int)(50 * 0.05 * Math.Min(taskCount, 20) / 20.0) : 0;
    int subPts = subTaskCount > 0 ? (int)(50 * 0.95 * Math.Min(subTaskCount, 100) / 100.0) : 0;
    Award(topPts + subPts, $"Planning ({taskCount} tasks, {subTaskCount} sub-tasks)");
}

// ============================================================
// PHASE 3: IMPLEMENTATION (working ADF examples)
// ============================================================

// Look for test results or verified ADF logs
var resultFiles = Directory.Exists(Path.Combine(root, "tests", "results"))
    ? Directory.GetFiles(Path.Combine(root, "tests", "results"), "*.txt").ToList()
    : new List<string>();

// Also check for test output markers
var testResultFiles = FindAllFiles(root, "*.trx");
int passedAdfs = 0;

// Check result files for PASS markers
foreach (var f in resultFiles)
{
    var content = File.ReadAllText(f);
    if (content.Contains("PASS", StringComparison.OrdinalIgnoreCase))
        passedAdfs++;
}

// Check test results from dotnet test
foreach (var f in testResultFiles)
{
    var content = File.ReadAllText(f);
    var passes = Regex.Matches(content, @"outcome=""Passed""", RegexOptions.IgnoreCase);
    passedAdfs += passes.Count;
}

// Also count any ADF files with corresponding .verified marker
var verifiedMarkers = FindAllFiles(root, "*.adf.verified");
passedAdfs += verifiedMarkers.Count;

if (passedAdfs > 0)
{
    // Scoring: 50, 45, 40, 35, 30, 29, 28, ..., 10, then 10x10, 5x9, 5x8, ..., then 1
    int adfScore = 0;
    for (int i = 0; i < passedAdfs; i++)
    {
        adfScore += AdfPoints(i);
    }
    Award(adfScore, $"Working ADF examples ({passedAdfs} verified)");
}

// ============================================================
// PHASE 4: TEST COVERAGE
// ============================================================

var testFiles = FindAllFiles(root, "*Test*.cs").Concat(FindAllFiles(root, "*Tests*.cs")).Distinct().ToList();
if (testFiles.Count > 0)
{
    Award(Math.Min(25, testFiles.Count * 2), $"Test files ({testFiles.Count} test files)");
}

// ============================================================
// PENALTY: Non-.NET source files
// ============================================================

var penaltyExtensions = new[] { ".py", ".js", ".ts", ".jsx", ".tsx", ".rb", ".go", ".java", ".rs" };
int penaltyFiles = 0;
foreach (var ext in penaltyExtensions)
{
    penaltyFiles += FindAllFiles(root, $"*{ext}")
        .Where(f => !f.Contains("node_modules") && !f.Contains(".git"))
        .Count();
}
if (penaltyFiles > 0)
{
    Award(-75 * penaltyFiles, $"Language penalty ({penaltyFiles} non-.NET source files)");
}

// ============================================================
// OUTPUT
// ============================================================

// Print score (the LAST line with just a number is what autoresearch reads)
foreach (var d in details)
    Console.Error.WriteLine(d);
Console.Error.WriteLine($"  Total: {score}");
Console.WriteLine(Math.Max(0, score));

// ============================================================
// HELPER FUNCTIONS
// ============================================================

static int AdfPoints(int index)
{
    // 0:50, 1:45, 2:40, 3:35, 4:30
    if (index < 5) return 50 - index * 5;
    // 5:29, 6:28, ..., 24:10
    if (index < 25) return 30 - (index - 4);
    // 25-34: 10 each (10 cycles)
    if (index < 35) return 10;
    // 35-39: 9 each (5 cycles)
    if (index < 40) return 9;
    // 40-44: 8 each (5 cycles)
    if (index < 45) return 8;
    // 45-49: 7 each
    if (index < 50) return 7;
    // 50-54: 6 each
    if (index < 55) return 6;
    // 55-59: 5 each
    if (index < 60) return 5;
    // 60-64: 4 each
    if (index < 65) return 4;
    // 65-69: 3 each
    if (index < 70) return 3;
    // 70-74: 2 each
    if (index < 75) return 2;
    // 75+: 1 each
    return 1;
}

static string? FindDoc(string root, params string[] keywords)
{
    var docs = FindAllDocs(root, keywords);
    return docs.FirstOrDefault();
}

static List<string> FindAllDocs(string root, params string[] keywords)
{
    var results = new List<string>();
    var mdFiles = Directory.GetFiles(root, "*.md", SearchOption.AllDirectories)
        .Where(f => !f.Contains(".git"))
        .ToList();

    foreach (var f in mdFiles)
    {
        var name = Path.GetFileNameWithoutExtension(f).ToLowerInvariant();
        var dir = Path.GetDirectoryName(f)?.ToLowerInvariant() ?? "";
        if (keywords.Any(k => name.Contains(k) || dir.Contains(k)))
            results.Add(f);
    }
    return results;
}

static List<string> FindAllFiles(string root, string pattern)
{
    try
    {
        return Directory.GetFiles(root, pattern, SearchOption.AllDirectories)
            .Where(f => !f.Contains(".git") && !f.Contains("node_modules") && !f.Contains("bin") && !f.Contains("obj"))
            .ToList();
    }
    catch { return new List<string>(); }
}
