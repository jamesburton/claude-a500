namespace AmigaA500.Core;

/// <summary>
/// Central event bus for emulator state changes.
/// Components publish events, UI/debug tools subscribe.
/// </summary>
public sealed class EmulatorEvents
{
    public event Action? OnReset;
    public event Action? OnFrameComplete;
    public event Action<int>? OnInterrupt;
    public event Action<uint, ushort>? OnRegisterWrite;
    public event Action<uint>? OnBreakpoint;
    public event Action<string>? OnDiskInserted;
    public event Action<string>? OnDiskEjected;
    public event Action? OnFreezeRequested;
    public event Action<string>? OnError;

    public void RaiseReset() => OnReset?.Invoke();
    public void RaiseFrameComplete() => OnFrameComplete?.Invoke();
    public void RaiseInterrupt(int level) => OnInterrupt?.Invoke(level);
    public void RaiseRegisterWrite(uint offset, ushort value) => OnRegisterWrite?.Invoke(offset, value);
    public void RaiseBreakpoint(uint address) => OnBreakpoint?.Invoke(address);
    public void RaiseDiskInserted(string name) => OnDiskInserted?.Invoke(name);
    public void RaiseDiskEjected(string name) => OnDiskEjected?.Invoke(name);
    public void RaiseFreezeRequested() => OnFreezeRequested?.Invoke();
    public void RaiseError(string message) => OnError?.Invoke(message);
}
