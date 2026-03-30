using AmigaA500.Core.Chipset;
using AmigaA500.Core.Input;

namespace AmigaA500.Core.Testing;

/// <summary>
/// Accelerated emulation runner for deep testing.
/// Runs the emulator at maximum speed (no frame limiting), captures screenshots
/// at specified intervals, and applies simulated input sequences.
/// </summary>
public sealed class AcceleratedRunner
{
    private readonly Amiga _amiga;
    private readonly SimulatedInput? _input;
    private readonly Keyboard _keyboard;
    private readonly Joystick _joystick;

    public long FramesExecuted { get; private set; }
    public long TotalCpuCycles => _amiga.Cpu.TotalCycles;
    public bool Halted => _amiga.Cpu.Halted;
    public uint CurrentPC => _amiga.Cpu.PC;

    // Screenshot capture
    private readonly List<(long frame, uint hash, string? path)> _captures = new();

    public AcceleratedRunner(Amiga amiga, SimulatedInput? input = null)
    {
        _amiga = amiga;
        _input = input;
        _keyboard = new Keyboard();
        _joystick = new Joystick();
        _input?.Prepare();
    }

    /// <summary>
    /// Run for a specified number of frames at maximum speed.
    /// Captures screenshots at intervals if captureInterval > 0.
    /// </summary>
    public void RunFrames(int count, int captureInterval = 0, string? captureDir = null)
    {
        if (captureDir != null) Directory.CreateDirectory(captureDir);

        for (int i = 0; i < count; i++)
        {
            // Apply simulated input for this frame
            if (_input != null)
            {
                var events = _input.GetEventsForFrame(FramesExecuted);
                SimulatedInput.ApplyEvents(events, _keyboard, _joystick);
            }

            // Run one frame
            _amiga.RunFrame();
            FramesExecuted++;

            // Capture screenshot if needed
            if (captureInterval > 0 && FramesExecuted % captureInterval == 0)
            {
                var capture = new ScreenCapture(_amiga.Framebuffer);
                uint hash = capture.GetHash();

                string? path = null;
                if (captureDir != null)
                {
                    path = Path.Combine(captureDir, $"frame_{FramesExecuted:D6}.bmp");
                    capture.SaveBmp(path);
                }

                _captures.Add((FramesExecuted, hash, path));
            }
        }
    }

    /// <summary>
    /// Run until a condition is met or timeout.
    /// </summary>
    public bool RunUntil(Func<bool> condition, int maxFrames = 10000)
    {
        for (int i = 0; i < maxFrames; i++)
        {
            if (_input != null)
            {
                var events = _input.GetEventsForFrame(FramesExecuted);
                SimulatedInput.ApplyEvents(events, _keyboard, _joystick);
            }

            _amiga.RunFrame();
            FramesExecuted++;

            if (condition()) return true;
        }
        return false; // Timed out
    }

    /// <summary>
    /// Run until the framebuffer changes from its current state.
    /// Useful for detecting when a new screen appears (title screen, menu, etc.)
    /// </summary>
    public bool RunUntilScreenChanges(int maxFrames = 500)
    {
        var capture = new ScreenCapture(_amiga.Framebuffer);
        uint initialHash = capture.GetHash();

        return RunUntil(() =>
        {
            var current = new ScreenCapture(_amiga.Framebuffer);
            return current.GetHash() != initialHash;
        }, maxFrames);
    }

    /// <summary>
    /// Run until the framebuffer stabilizes (same content for N consecutive frames).
    /// Useful for waiting until a static screen (title, menu) is fully rendered.
    /// </summary>
    public bool RunUntilStable(int stableFrames = 10, int maxFrames = 500)
    {
        uint lastHash = 0;
        int consecutiveSame = 0;

        return RunUntil(() =>
        {
            var capture = new ScreenCapture(_amiga.Framebuffer);
            uint hash = capture.GetHash();
            if (hash == lastHash)
            {
                consecutiveSame++;
                return consecutiveSame >= stableFrames;
            }
            lastHash = hash;
            consecutiveSame = 0;
            return false;
        }, maxFrames);
    }

    /// <summary>
    /// Check if the framebuffer looks like a valid image (not blank, has variety).
    /// </summary>
    public bool FramebufferLooksValid()
    {
        var capture = new ScreenCapture(_amiga.Framebuffer);
        if (capture.IsBlank()) return false;

        // Check that there are at least a few distinct colors
        var colors = new HashSet<uint>();
        for (int i = 0; i < Math.Min(_amiga.Framebuffer.Length, 320 * 256); i += 4)
        {
            colors.Add(_amiga.Framebuffer[i]);
            if (colors.Count >= 4) return true; // At least 4 colors = looks like real content
        }
        return colors.Count >= 2;
    }

    /// <summary>
    /// Capture the current framebuffer as a title screen.
    /// </summary>
    public string? CaptureTitleScreen(string outputDir, string gameName)
    {
        Directory.CreateDirectory(outputDir);
        var capture = new ScreenCapture(_amiga.Framebuffer);

        if (capture.IsBlank()) return null;

        string safeName = string.Join("_", gameName.Split(Path.GetInvalidFileNameChars()));
        string bmpPath = Path.Combine(outputDir, $"{safeName}.bmp");
        string verifiedPath = Path.Combine(outputDir, $"{safeName}.verified");

        capture.SaveBmp(bmpPath);
        File.WriteAllText(verifiedPath, $"Title screen: {gameName}\nFrame: {FramesExecuted}\nHash: {capture.GetHash():X8}\nBlank: {capture.IsBlank()}\nDate: {DateTime.Now:O}");

        return bmpPath;
    }

    public IReadOnlyList<(long frame, uint hash, string? path)> Captures => _captures;
}
