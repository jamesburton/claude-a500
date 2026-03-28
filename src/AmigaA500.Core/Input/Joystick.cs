namespace AmigaA500.Core.Input;

/// <summary>
/// Amiga joystick/mouse port emulation.
/// </summary>
public sealed class Joystick
{
    public bool Up, Down, Left, Right;
    public bool Fire1, Fire2, Fire3;

    // Mouse state
    private byte _mouseX, _mouseY;

    public void MoveMouse(int dx, int dy)
    {
        _mouseX = (byte)(_mouseX + dx);
        _mouseY = (byte)(_mouseY + dy);
    }

    /// <summary>
    /// Read JOYxDAT register value. Encodes position as quadrature counters.
    /// </summary>
    public ushort ReadJoyDat()
    {
        // For joystick: encode directions as quadrature-style bits
        int v1 = Down ? 1 : 0;
        int v0 = (Down != Up) ? 1 : 0; // XOR
        int h1 = Right ? 1 : 0;
        int h0 = (Right != Left) ? 1 : 0;
        return (ushort)((v1 << 9) | (v0 << 8) | (h1 << 1) | h0);
    }

    /// <summary>
    /// Read JOYxDAT for mouse mode — returns quadrature counters.
    /// </summary>
    public ushort ReadMouseDat()
    {
        return (ushort)(_mouseY << 8 | _mouseX);
    }
}
