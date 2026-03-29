namespace AmigaA500.Core;

/// <summary>
/// Emulator configuration — controls hardware options and emulation behavior.
/// </summary>
public sealed class Configuration
{
    // Hardware
    public int ChipRamSize { get; set; } = 512 * 1024;  // 512KB default
    public int SlowRamSize { get; set; } = 0;             // No slow RAM by default
    public int FastRamSize { get; set; } = 0;             // No fast RAM by default
    public bool IsPal { get; set; } = true;

    // ROM
    public string? KickstartRomPath { get; set; }

    // Drives
    public string?[] DiskPaths { get; set; } = new string?[4];

    // Emulation
    public bool AccelerateLoops { get; set; } = true;
    public bool AccurateCycleCounting { get; set; } = false;
    public int FrameSkip { get; set; } = 0;

    // Audio
    public int AudioSampleRate { get; set; } = 44100;
    public int AudioBufferSize { get; set; } = 4096;
    public bool AudioEnabled { get; set; } = true;

    // Video
    public int DisplayScale { get; set; } = 2;
    public bool Fullscreen { get; set; } = false;
    public bool ShowFps { get; set; } = false;

    // Debug
    public bool TraceEnabled { get; set; } = false;
    public bool ProfileEnabled { get; set; } = false;

    public static Configuration Default => new();

    public static Configuration FromArgs(string[] args)
    {
        var config = new Configuration();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--rom" when i + 1 < args.Length: config.KickstartRomPath = args[++i]; break;
                case "--disk" or "--df0" when i + 1 < args.Length: config.DiskPaths[0] = args[++i]; break;
                case "--df1" when i + 1 < args.Length: config.DiskPaths[1] = args[++i]; break;
                case "--pal": config.IsPal = true; break;
                case "--ntsc": config.IsPal = false; break;
                case "--chip" when i + 1 < args.Length: config.ChipRamSize = int.Parse(args[++i]) * 1024; break;
                case "--slow" when i + 1 < args.Length: config.SlowRamSize = int.Parse(args[++i]) * 1024; break;
                case "--fast" when i + 1 < args.Length: config.FastRamSize = int.Parse(args[++i]) * 1024; break;
                case "--scale" when i + 1 < args.Length: config.DisplayScale = int.Parse(args[++i]); break;
                case "--fullscreen": config.Fullscreen = true; break;
                case "--fps": config.ShowFps = true; break;
                case "--trace": config.TraceEnabled = true; break;
                case "--profile": config.ProfileEnabled = true; break;
                case "--no-audio": config.AudioEnabled = false; break;
                case "--no-accel": config.AccelerateLoops = false; break;
            }
        }
        return config;
    }
}
