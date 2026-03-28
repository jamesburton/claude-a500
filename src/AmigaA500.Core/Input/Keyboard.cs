namespace AmigaA500.Core.Input;

/// <summary>
/// Amiga keyboard interface — translates host keycodes to Amiga scan codes.
/// </summary>
public sealed class Keyboard
{
    private readonly Queue<byte> _buffer = new();
    private readonly Action? _onKeyReady;

    public Keyboard(Action? onKeyReady = null)
    {
        _onKeyReady = onKeyReady;
    }

    public void KeyDown(AmigaKey key)
    {
        byte code = (byte)((byte)key << 1); // Transmitted code: scancode << 1, inverted by keyboard MCU
        _buffer.Enqueue((byte)~code);
        _onKeyReady?.Invoke();
    }

    public void KeyUp(AmigaKey key)
    {
        byte code = (byte)(((byte)key << 1) | 1); // Bit 0 = 1 for release
        _buffer.Enqueue((byte)~code);
        _onKeyReady?.Invoke();
    }

    public bool HasData => _buffer.Count > 0;

    public byte ReadData()
    {
        return _buffer.Count > 0 ? _buffer.Dequeue() : (byte)0xFF;
    }

    /// <summary>
    /// Decode the raw keyboard data to recover the scan code.
    /// The keyboard MCU inverts and rotates the code.
    /// </summary>
    public static (byte scanCode, bool released) DecodeRawData(byte raw)
    {
        byte decoded = (byte)~raw;
        bool released = (decoded & 1) != 0;
        byte scanCode = (byte)(decoded >> 1);
        return (scanCode, released);
    }
}

/// <summary>
/// Amiga raw keyboard scan codes.
/// </summary>
public enum AmigaKey : byte
{
    Tilde = 0x00,
    Key1 = 0x01, Key2 = 0x02, Key3 = 0x03, Key4 = 0x04, Key5 = 0x05,
    Key6 = 0x06, Key7 = 0x07, Key8 = 0x08, Key9 = 0x09, Key0 = 0x0A,
    Minus = 0x0B, Equals = 0x0C, Backslash = 0x0D, Backspace = 0x41,
    Tab = 0x42,
    Q = 0x10, W = 0x11, E = 0x12, R = 0x13, T = 0x14,
    Y = 0x15, U = 0x16, I = 0x17, O = 0x18, P = 0x19,
    LeftBracket = 0x1A, RightBracket = 0x1B,
    Return = 0x44,
    CapsLock = 0x62,
    A = 0x20, S = 0x21, D = 0x22, F = 0x23, G = 0x24,
    H = 0x25, J = 0x26, K = 0x27, L = 0x28,
    Semicolon = 0x29, Apostrophe = 0x2A,
    LeftShift = 0x60,
    Z = 0x31, X = 0x32, C = 0x33, V = 0x34, B = 0x35,
    N = 0x36, M = 0x37, Comma = 0x38, Period = 0x39, Slash = 0x3A,
    RightShift = 0x61,
    LeftAlt = 0x64, RightAlt = 0x65,
    LeftAmiga = 0x66, RightAmiga = 0x67,
    Space = 0x40,
    Delete = 0x46,
    Up = 0x4C, Down = 0x4D, Left = 0x4F, Right = 0x4E,
    F1 = 0x50, F2 = 0x51, F3 = 0x52, F4 = 0x53, F5 = 0x54,
    F6 = 0x55, F7 = 0x56, F8 = 0x57, F9 = 0x58, F10 = 0x59,
    Escape = 0x45,
    Help = 0x5F,
    // Numeric keypad
    Num0 = 0x0F, Num1 = 0x1D, Num2 = 0x1E, Num3 = 0x1F,
    Num4 = 0x2D, Num5 = 0x2E, Num6 = 0x2F,
    Num7 = 0x3D, Num8 = 0x3E, Num9 = 0x3F,
    NumPeriod = 0x3C, NumMinus = 0x4A,
    NumEnter = 0x43, NumLeftParen = 0x5A, NumRightParen = 0x5B,
    NumSlash = 0x5C, NumStar = 0x5D, NumPlus = 0x5E,
}
