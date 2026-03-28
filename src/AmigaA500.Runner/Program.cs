using AmigaA500.Core;
using AmigaA500.Core.Floppy;

if (args.Length < 1)
{
    Console.WriteLine("Amiga 500 Emulator");
    Console.WriteLine("Usage: AmigaA500.Runner <kickstart.rom> [disk.adf]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --frames <n>   Run for n frames then exit (default: 10)");
    Console.WriteLine("  --trace        Enable CPU instruction trace");
    return;
}

string romPath = args[0];
string? adfPath = args.Length > 1 && !args[1].StartsWith("--") ? args[1] : null;
int maxFrames = 10;
bool trace = false;

for (int i = 1; i < args.Length; i++)
{
    if (args[i] == "--frames" && i + 1 < args.Length)
        maxFrames = int.Parse(args[++i]);
    else if (args[i] == "--trace")
        trace = true;
}

// Load Kickstart ROM
if (!File.Exists(romPath))
{
    Console.Error.WriteLine($"ROM not found: {romPath}");
    return;
}

byte[] romData = File.ReadAllBytes(romPath);
Console.WriteLine($"Loaded ROM: {romPath} ({romData.Length} bytes)");

// Create Amiga system
var amiga = new Amiga(romData);

// Load ADF if provided
if (adfPath != null && File.Exists(adfPath))
{
    var disk = AdfDisk.Load(adfPath);
    amiga.InsertDisk(0, disk);
    Console.WriteLine($"Loaded disk: {adfPath} ({(disk.IsBootable() ? "bootable" : "not bootable")})");
}

// Reset and run
amiga.Reset();
Console.WriteLine($"CPU initialized: PC=${amiga.Cpu.PC:X6}, SP=${amiga.Cpu.A[7]:X6}");
Console.WriteLine($"Running {maxFrames} frames...");

var sw = System.Diagnostics.Stopwatch.StartNew();

for (int frame = 0; frame < maxFrames; frame++)
{
    long frameStart = amiga.Cpu.TotalCycles;

    try
    {
        amiga.RunFrame();
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Frame {frame}: Exception at PC=${amiga.Cpu.PC:X6}: {ex.Message}");
        break;
    }

    long frameCycles = amiga.Cpu.TotalCycles - frameStart;

    if (trace || frame % 100 == 0)
    {
        Console.WriteLine($"Frame {frame}: PC=${amiga.Cpu.PC:X6} D0=${amiga.Cpu.D[0]:X8} cycles={frameCycles}");
    }
}

sw.Stop();
Console.WriteLine();
Console.WriteLine($"Completed {maxFrames} frames in {sw.ElapsedMilliseconds}ms");
Console.WriteLine($"Total CPU cycles: {amiga.Cpu.TotalCycles:N0}");
Console.WriteLine($"Final state: PC=${amiga.Cpu.PC:X6} SR=${amiga.Cpu.SR:X4}");
for (int i = 0; i < 8; i++)
    Console.Write($"D{i}=${amiga.Cpu.D[i]:X8} ");
Console.WriteLine();
for (int i = 0; i < 8; i++)
    Console.Write($"A{i}=${amiga.Cpu.A[i]:X8} ");
Console.WriteLine();
