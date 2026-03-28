// ADF verification tool — tests whether the emulator can load and begin executing ADF images
// Usage: dotnet run tools/verify-adf.cs <kickstart.rom> <disk.adf> [--frames N]
// Outputs: BOOT/PASS/FAIL <disk-name>
// .NET 10 single-file script

#r "src/AmigaA500.Core/bin/Debug/net10.0/AmigaA500.Core.dll"

using AmigaA500.Core;
using AmigaA500.Core.Floppy;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: dotnet run tools/verify-adf.cs <kickstart.rom> <disk.adf> [--frames N]");
    return;
}

string romPath = args[0];
string adfPath = args[1];
int maxFrames = 5;

for (int i = 2; i < args.Length; i++)
    if (args[i] == "--frames" && i + 1 < args.Length)
        maxFrames = int.Parse(args[++i]);

if (!File.Exists(romPath)) { Console.WriteLine($"FAIL {Path.GetFileNameWithoutExtension(adfPath)} (ROM not found)"); return; }
if (!File.Exists(adfPath)) { Console.WriteLine($"FAIL {Path.GetFileNameWithoutExtension(adfPath)} (ADF not found)"); return; }

string diskName = Path.GetFileNameWithoutExtension(adfPath);

try
{
    byte[] romData = File.ReadAllBytes(romPath);
    byte[] adfData = File.ReadAllBytes(adfPath);

    if (adfData.Length != 901120)
    {
        Console.WriteLine($"FAIL {diskName} (bad size: {adfData.Length})");
        return;
    }

    var disk = new AdfDisk(adfData);
    var amiga = new Amiga(romData);
    amiga.InsertDisk(0, disk);
    amiga.Reset();

    uint initialPC = amiga.Cpu.PC;
    bool pcChanged = false;
    bool exception = false;

    for (int frame = 0; frame < maxFrames; frame++)
    {
        try
        {
            amiga.RunFrame();
            if (amiga.Cpu.PC != initialPC) pcChanged = true;
        }
        catch
        {
            exception = true;
            break;
        }
    }

    if (exception)
        Console.WriteLine($"FAIL {diskName} (exception during execution)");
    else if (disk.IsBootable() && pcChanged)
        Console.WriteLine($"BOOT {diskName}");
    else if (pcChanged)
        Console.WriteLine($"PASS {diskName}");
    else
        Console.WriteLine($"FAIL {diskName} (PC stuck at ${amiga.Cpu.PC:X6})");
}
catch (Exception ex)
{
    Console.WriteLine($"FAIL {diskName} ({ex.Message})");
}
