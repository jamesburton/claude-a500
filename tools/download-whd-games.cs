// Downloads WHD game disk images from archive.org/download/Amiga_WHD_Games
// These are full game disk images in various archive formats
// Usage: dotnet run tools/download-whd-games.cs [--max N]
// .NET 10 single-file script

using System.Net.Http;
using System.Text.RegularExpressions;

int maxDownloads = 100;
for (int i = 0; i < args.Length; i++)
    if (args[i] == "--max" && i + 1 < args.Length) maxDownloads = int.Parse(args[++i]);

string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "firmware", "whd-games");
Directory.CreateDirectory(outputDir);

string baseUrl = "https://archive.org/download/Amiga_WHD_Games/";
Console.Error.WriteLine($"Fetching file list from: {baseUrl}");

using var http = new HttpClient();
http.Timeout = TimeSpan.FromSeconds(60);

try
{
    // Get the directory listing page
    var html = await http.GetStringAsync(baseUrl);

    // Parse file links — look for .lha and .zip files
    var linkPattern = new Regex(@"href=""([^""]+\.(lha|zip|lzh))""", RegexOptions.IgnoreCase);
    var matches = linkPattern.Matches(html);

    var files = matches.Select(m => m.Groups[1].Value)
        .Where(f => !f.Contains("..") && !f.StartsWith("/"))
        .Distinct()
        .OrderBy(f => f)
        .ToList();

    Console.Error.WriteLine($"Found {files.Count} game archives");

    int downloaded = 0, skipped = 0;

    foreach (var file in files)
    {
        if (downloaded >= maxDownloads) break;

        string localPath = Path.Combine(outputDir, file);
        if (File.Exists(localPath)) { skipped++; continue; }

        string url = baseUrl + Uri.EscapeDataString(file);
        Console.Error.Write($"  {file}... ");

        try
        {
            var data = await http.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(localPath, data);
            Console.Error.WriteLine($"OK ({data.Length:N0} bytes)");
            downloaded++;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAIL ({ex.Message})");
        }
    }

    Console.Error.WriteLine($"\nComplete: {downloaded} downloaded, {skipped} already present");
    Console.WriteLine(downloaded);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Console.WriteLine(0);
}
