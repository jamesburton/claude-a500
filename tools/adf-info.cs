// ADF disk information tool
// Usage: dotnet run tools/adf-info.cs <file.adf>
// .NET 10 single-file script

if (args.Length < 1) { Console.WriteLine("Usage: dotnet run tools/adf-info.cs <file.adf>"); return; }

string path = args[0];
if (!File.Exists(path)) { Console.Error.WriteLine($"File not found: {path}"); return; }

byte[] data = File.ReadAllBytes(path);
Console.WriteLine($"File: {Path.GetFileName(path)}");
Console.WriteLine($"Size: {data.Length:N0} bytes ({data.Length / 1024} KB)");

if (data.Length != 901120)
{
    Console.WriteLine("WARNING: Not a standard ADF (expected 901,120 bytes)");
    return;
}

// Disk type
string diskType = (data[0], data[1], data[2], data[3]) switch
{
    ((byte)'D', (byte)'O', (byte)'S', 0) => "OFS (Old File System)",
    ((byte)'D', (byte)'O', (byte)'S', 1) => "FFS (Fast File System)",
    ((byte)'D', (byte)'O', (byte)'S', 2) => "OFS International",
    ((byte)'D', (byte)'O', (byte)'S', 3) => "FFS International",
    _ => $"Unknown ({data[0]:X2}{data[1]:X2}{data[2]:X2}{data[3]:X2})"
};
Console.WriteLine($"Type: {diskType}");

// Bootblock checksum
uint checksum = 0;
for (int i = 0; i < 1024; i += 4)
{
    uint word = (uint)(data[i] << 24 | data[i + 1] << 16 | data[i + 2] << 8 | data[i + 3]);
    uint prev = checksum;
    checksum += word;
    if (checksum < prev) checksum++;
}
bool bootable = checksum == 0xFFFFFFFF;
Console.WriteLine($"Bootable: {(bootable ? "Yes" : "No")}");

// Root block (track 40, sector 0 — middle of disk)
int rootOffset = 80 * 11 * 512; // Track 80 (cyl 40, side 0)
string rootName = "";
int nameLen = data[rootOffset + 432];
if (nameLen > 0 && nameLen < 31)
{
    for (int i = 0; i < nameLen; i++)
        rootName += (char)data[rootOffset + 433 + i];
    Console.WriteLine($"Volume: {rootName}");
}

// Stats
Console.WriteLine($"Tracks: 160 (80 cylinders × 2 sides)");
Console.WriteLine($"Sectors/track: 11");
Console.WriteLine($"Bytes/sector: 512");

// Non-zero sector count
int nonZeroSectors = 0;
for (int t = 0; t < 160; t++)
{
    for (int s = 0; s < 11; s++)
    {
        int offset = (t * 11 + s) * 512;
        bool empty = true;
        for (int i = 0; i < 512 && empty; i++)
            if (data[offset + i] != 0) empty = false;
        if (!empty) nonZeroSectors++;
    }
}
Console.WriteLine($"Used sectors: {nonZeroSectors}/1760 ({100 * nonZeroSectors / 1760}%)");
