// Downloads public domain ADF files from Internet Archive
// Usage: dotnet run tools/download-pd-adfs.cs [--max N] [--category games|demos|apps]
// Requires: internet access
// .NET 10 single-file script

using System.Net.Http;
using System.Text.Json;

int maxDownloads = 50;
string category = "games";
string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "firmware", "pd-adfs");

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--max" && i + 1 < args.Length) maxDownloads = int.Parse(args[++i]);
    if (args[i] == "--category" && i + 1 < args.Length) category = args[++i];
}

Directory.CreateDirectory(outputDir);

string collection = category switch
{
    "games" => "commodore-amiga-games-public-domain-adf",
    "demos" => "commodore-amiga-demos-various-adf",
    "apps" => "commodore-amiga-applications-public-domain-adf",
    _ => "commodore-amiga-games-public-domain-adf"
};

string metadataUrl = $"https://archive.org/metadata/{collection}";
Console.Error.WriteLine($"Fetching file list from: {collection}");

using var http = new HttpClient();
http.Timeout = TimeSpan.FromSeconds(30);

try
{
    var json = await http.GetStringAsync(metadataUrl);
    var doc = JsonDocument.Parse(json);
    var files = doc.RootElement.GetProperty("files");

    int downloaded = 0;
    int skipped = 0;

    foreach (var file in files.EnumerateArray())
    {
        if (downloaded >= maxDownloads) break;

        string name = file.GetProperty("name").GetString() ?? "";
        if (!name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) continue;

        string baseName = Path.GetFileNameWithoutExtension(name);
        string localPath = Path.Combine(outputDir, name);

        if (File.Exists(localPath))
        {
            skipped++;
            continue;
        }

        string downloadUrl = $"https://archive.org/download/{collection}/{Uri.EscapeDataString(name)}";
        Console.Error.Write($"  Downloading: {baseName}... ");

        try
        {
            var data = await http.GetByteArrayAsync(downloadUrl);
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
    Console.Error.WriteLine($"Error fetching metadata: {ex.Message}");
    Console.WriteLine(0);
}
