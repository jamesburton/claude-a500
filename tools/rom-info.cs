// Kickstart ROM information tool
// Usage: dotnet run tools/rom-info.cs <kickstart.rom>
// .NET 10 single-file script

if (args.Length < 1) { Console.WriteLine("Usage: dotnet run tools/rom-info.cs <kickstart.rom>"); return; }

string path = args[0];
if (!File.Exists(path)) { Console.Error.WriteLine($"File not found: {path}"); return; }

byte[] rom = File.ReadAllBytes(path);
Console.WriteLine($"File: {Path.GetFileName(path)}");
Console.WriteLine($"Size: {rom.Length:N0} bytes ({rom.Length / 1024} KB)");

// Reset vectors
uint ssp = (uint)(rom[0] << 24 | rom[1] << 16 | rom[2] << 8 | rom[3]);
uint pc = (uint)(rom[4] << 24 | rom[5] << 16 | rom[6] << 8 | rom[7]);
Console.WriteLine($"Reset SSP: ${ssp:X8}");
Console.WriteLine($"Reset PC:  ${pc:X8}");

// Detect Kickstart version by searching for version string
string? version = null;
for (int i = 0; i < rom.Length - 20; i++)
{
    // Look for "Kickstart" string
    if (rom[i] == 'K' && rom[i + 1] == 'i' && rom[i + 2] == 'c' && rom[i + 3] == 'k')
    {
        int end = i;
        while (end < rom.Length && end < i + 50 && rom[end] >= 0x20 && rom[end] < 0x7F) end++;
        version = System.Text.Encoding.ASCII.GetString(rom, i, end - i);
        break;
    }
}
if (version != null) Console.WriteLine($"Version: {version}");

// ROM checksum
uint checksum = 0;
for (int i = 0; i < rom.Length; i += 2)
{
    uint word = (uint)(rom[i] << 8 | rom[i + 1]);
    uint prev = checksum;
    checksum += word;
    if (checksum < prev) checksum++;
}
Console.WriteLine($"Checksum: ${checksum:X8}");

// Known ROM sizes
string sizeInfo = rom.Length switch
{
    262144 => "256 KB (Kickstart 1.x-3.x standard)",
    524288 => "512 KB (Kickstart 3.x extended or A1000 doubled)",
    _ => "non-standard"
};
Console.WriteLine($"Size class: {sizeInfo}");
