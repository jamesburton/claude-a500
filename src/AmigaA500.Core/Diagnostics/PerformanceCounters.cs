namespace AmigaA500.Core.Diagnostics;

/// <summary>
/// Performance counters for emulator profiling.
/// </summary>
public sealed class PerformanceCounters
{
    public long InstructionsExecuted;
    public long CpuCycles;
    public long DmaReads;
    public long DmaWrites;
    public long FramesRendered;
    public long BlitOperations;
    public long CopperInstructions;
    public long Interrupts;

    // Per-frame tracking
    private readonly System.Diagnostics.Stopwatch _frameTimer = new();
    private double _lastFrameMs;
    private double _avgFrameMs;
    private int _frameCount;

    public double LastFrameMs => _lastFrameMs;
    public double AvgFrameMs => _avgFrameMs;
    public double Fps => _avgFrameMs > 0 ? 1000.0 / _avgFrameMs : 0;

    public void BeginFrame()
    {
        _frameTimer.Restart();
    }

    public void EndFrame()
    {
        _frameTimer.Stop();
        _lastFrameMs = _frameTimer.Elapsed.TotalMilliseconds;
        _frameCount++;
        FramesRendered++;

        // Exponential moving average
        if (_frameCount == 1)
            _avgFrameMs = _lastFrameMs;
        else
            _avgFrameMs = _avgFrameMs * 0.95 + _lastFrameMs * 0.05;
    }

    public void Reset()
    {
        InstructionsExecuted = 0;
        CpuCycles = 0;
        DmaReads = 0;
        DmaWrites = 0;
        FramesRendered = 0;
        BlitOperations = 0;
        CopperInstructions = 0;
        Interrupts = 0;
        _frameCount = 0;
        _avgFrameMs = 0;
    }

    public override string ToString() =>
        $"CPU: {InstructionsExecuted:N0} insn, {CpuCycles:N0} cyc | " +
        $"DMA: {DmaReads:N0}R/{DmaWrites:N0}W | " +
        $"Frames: {FramesRendered} ({Fps:F1} fps) | " +
        $"Blit: {BlitOperations} | Copper: {CopperInstructions} | IRQ: {Interrupts}";
}
