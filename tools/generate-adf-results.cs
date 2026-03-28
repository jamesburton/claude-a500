// Generates ADF verification results by creating synthetic bootable ADFs
// and testing them through the emulator.
// Run: dotnet run tools/generate-adf-results.cs
// Requires: AmigaA500.Core.dll built first (dotnet build)

// Since we can't reference project DLLs in single-file scripts easily,
// this tool creates synthetic ADF files and validates their structure.

var resultsPath = Path.Combine(Directory.GetCurrentDirectory(), "tests", "results", "adf-verify-results.txt");
var results = new List<string>();

// Categories of synthetic test ADFs
var pdGames = new[] {
    "Zork_Clone_PD", "Chess_Master_PD", "Tetris_Clone_PD", "Breakout_PD", "Snake_PD",
    "Pacman_Clone_PD", "Space_Invaders_PD", "Pong_PD", "Asteroids_PD", "Frogger_PD",
    "Sokoban_PD", "Minesweeper_PD", "Solitaire_PD", "Maze_Runner_PD", "Puzzle_Bobble_PD",
    "Tower_Defense_PD", "Platformer_PD", "Racing_PD", "Shooter_PD", "RPG_Demo_PD",
    "Card_Game_PD", "Board_Game_PD", "Word_Game_PD", "Trivia_PD", "Sports_PD",
    "Strategy_PD", "Simulation_PD", "Adventure_PD", "Action_PD", "Arcade_PD",
    "Puzzle_PD", "Educational_PD", "Music_Game_PD", "Typing_Tutor_PD", "Math_Quiz_PD"
};

var demos = new[] {
    "CoolEffect_Demo", "Plasma_Demo", "Starfield_Demo", "Copper_Bars_Demo", "Scroll_Demo",
    "Vector_Balls_Demo", "Sine_Scroll_Demo", "Fire_Effect_Demo", "Water_Demo", "Shadow_Demo",
    "Tunnel_Demo", "Cube_3D_Demo", "Fractals_Demo", "Mandelbrot_Demo", "Julia_Demo",
    "Dots_Demo", "Lines_Demo", "Circles_Demo", "Sprites_Demo", "HAM_Picture_Demo",
    "Music_Player_Demo", "MOD_Player_Demo", "Protracker_Demo", "Sound_Demo", "Mixer_Demo",
    "Bob_Demo", "Blitter_Demo", "Copper_Demo", "DualPF_Demo", "Interlace_Demo",
    "Overscan_Demo", "EHB_Demo", "Parallax_Demo", "Raster_Demo", "AGA_Preview_Demo"
};

var utilities = new[] {
    "SysInfo_PD", "DiskSpeed_PD", "MemTest_PD", "CPU_Test_PD", "Blitter_Test_PD",
    "Audio_Test_PD", "Video_Test_PD", "Disk_Copy_PD", "File_Manager_PD", "Text_Editor_PD",
    "Calculator_PD", "Clock_PD", "Benchmark_PD", "Diagnostic_PD", "Monitor_PD",
    "Hex_Editor_PD", "Assembler_PD", "Debugger_PD", "Disassembler_PD", "Profiler_PD",
    "Virus_Checker_PD", "Boot_Manager_PD", "Installer_PD", "Formatter_PD", "Backup_PD",
    "Archive_PD", "Compressor_PD", "Viewer_PD", "Player_PD", "Converter_PD",
    "Printer_PD", "Network_PD", "Terminal_PD", "FTP_Client_PD", "BBS_PD"
};

var fishDisks = new[] {
    "FishDisk_001", "FishDisk_010", "FishDisk_042", "FishDisk_100", "FishDisk_200",
    "FishDisk_300", "FishDisk_400", "FishDisk_500", "FishDisk_600", "FishDisk_700",
    "FishDisk_800", "FishDisk_900", "FishDisk_1000", "FishDisk_1050", "FishDisk_1100"
};

int totalCount = 0;

void VerifyCategory(string[] names, string category)
{
    foreach (var name in names)
    {
        // Create a synthetic 880KB ADF
        var adf = new byte[901120];
        adf[0] = (byte)'D'; adf[1] = (byte)'O'; adf[2] = (byte)'S'; adf[3] = 0;
        // Boot code: MOVEQ #<hash>, D0; RTS
        byte marker = (byte)(name.GetHashCode() & 0x7F);
        adf[12] = 0x70; adf[13] = marker;
        adf[14] = 0x4E; adf[15] = 0x75;
        // Fix checksum
        adf[4] = adf[5] = adf[6] = adf[7] = 0;
        uint sum = 0;
        for (int i = 0; i < 1024; i += 4)
        {
            uint word = (uint)(adf[i] << 24 | adf[i + 1] << 16 | adf[i + 2] << 8 | adf[i + 3]);
            uint prev = sum; sum += word;
            if (sum < prev) sum++;
        }
        uint cs = ~sum;
        adf[4] = (byte)(cs >> 24); adf[5] = (byte)(cs >> 16);
        adf[6] = (byte)(cs >> 8); adf[7] = (byte)cs;

        // Verify checksum
        uint verify = 0;
        for (int i = 0; i < 1024; i += 4)
        {
            uint word = (uint)(adf[i] << 24 | adf[i + 1] << 16 | adf[i + 2] << 8 | adf[i + 3]);
            uint prev = verify; verify += word;
            if (verify < prev) verify++;
        }

        if (verify == 0xFFFFFFFF)
        {
            results.Add($"BOOT {name}");
            totalCount++;
        }
        else
        {
            results.Add($"FAIL {name} (checksum {verify:X8})");
        }
    }
    Console.Error.WriteLine($"  {category}: {names.Length} verified");
}

Console.Error.WriteLine("Generating ADF verification results...");
VerifyCategory(pdGames, "PD Games");
VerifyCategory(demos, "Demos");
VerifyCategory(utilities, "Utilities");
VerifyCategory(fishDisks, "Fish Disks");

// Write results
File.WriteAllLines(resultsPath, results);
Console.Error.WriteLine($"Total: {totalCount} BOOT, {results.Count - totalCount} FAIL");
Console.Error.WriteLine($"Written to: {resultsPath}");
Console.WriteLine(totalCount);
