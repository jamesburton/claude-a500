namespace AmigaA500.Core.Input;

/// <summary>
/// Maps host keyboard scan codes (SDL2/ConsoleKey) to Amiga raw key codes.
/// </summary>
public static class KeyboardMapper
{
    private static readonly Dictionary<int, AmigaKey> SdlToAmiga = new()
    {
        // Letters
        [97] = AmigaKey.A, [98] = AmigaKey.B, [99] = AmigaKey.C, [100] = AmigaKey.D,
        [101] = AmigaKey.E, [102] = AmigaKey.F, [103] = AmigaKey.G, [104] = AmigaKey.H,
        [105] = AmigaKey.I, [106] = AmigaKey.J, [107] = AmigaKey.K, [108] = AmigaKey.L,
        [109] = AmigaKey.M, [110] = AmigaKey.N, [111] = AmigaKey.O, [112] = AmigaKey.P,
        [113] = AmigaKey.Q, [114] = AmigaKey.R, [115] = AmigaKey.S, [116] = AmigaKey.T,
        [117] = AmigaKey.U, [118] = AmigaKey.V, [119] = AmigaKey.W, [120] = AmigaKey.X,
        [121] = AmigaKey.Y, [122] = AmigaKey.Z,
        // Numbers
        [48] = AmigaKey.Key0, [49] = AmigaKey.Key1, [50] = AmigaKey.Key2,
        [51] = AmigaKey.Key3, [52] = AmigaKey.Key4, [53] = AmigaKey.Key5,
        [54] = AmigaKey.Key6, [55] = AmigaKey.Key7, [56] = AmigaKey.Key8, [57] = AmigaKey.Key9,
        // Special
        [13] = AmigaKey.Return, [27] = AmigaKey.Escape, [32] = AmigaKey.Space,
        [8] = AmigaKey.Backspace, [9] = AmigaKey.Tab, [127] = AmigaKey.Delete,
        // Arrow keys (SDL scan codes)
        [1073741906] = AmigaKey.Up, [1073741905] = AmigaKey.Down,
        [1073741904] = AmigaKey.Left, [1073741903] = AmigaKey.Right,
        // F-keys
        [1073741882] = AmigaKey.F1, [1073741883] = AmigaKey.F2,
        [1073741884] = AmigaKey.F3, [1073741885] = AmigaKey.F4,
        [1073741886] = AmigaKey.F5, [1073741887] = AmigaKey.F6,
        [1073741888] = AmigaKey.F7, [1073741889] = AmigaKey.F8,
        [1073741890] = AmigaKey.F9, [1073741891] = AmigaKey.F10,
        // Modifiers
        [1073742049] = AmigaKey.LeftShift, [1073742053] = AmigaKey.RightShift,
        [1073742050] = AmigaKey.LeftAlt, [1073742054] = AmigaKey.RightAlt,
    };

    public static AmigaKey? MapSdlKey(int sdlKeyCode)
    {
        return SdlToAmiga.TryGetValue(sdlKeyCode, out var key) ? key : null;
    }

    public static AmigaKey? MapConsoleKey(ConsoleKey key)
    {
        return key switch
        {
            ConsoleKey.A => AmigaKey.A, ConsoleKey.B => AmigaKey.B, ConsoleKey.C => AmigaKey.C,
            ConsoleKey.D => AmigaKey.D, ConsoleKey.E => AmigaKey.E, ConsoleKey.F => AmigaKey.F,
            ConsoleKey.G => AmigaKey.G, ConsoleKey.H => AmigaKey.H, ConsoleKey.I => AmigaKey.I,
            ConsoleKey.J => AmigaKey.J, ConsoleKey.K => AmigaKey.K, ConsoleKey.L => AmigaKey.L,
            ConsoleKey.M => AmigaKey.M, ConsoleKey.N => AmigaKey.N, ConsoleKey.O => AmigaKey.O,
            ConsoleKey.P => AmigaKey.P, ConsoleKey.Q => AmigaKey.Q, ConsoleKey.R => AmigaKey.R,
            ConsoleKey.S => AmigaKey.S, ConsoleKey.T => AmigaKey.T, ConsoleKey.U => AmigaKey.U,
            ConsoleKey.V => AmigaKey.V, ConsoleKey.W => AmigaKey.W, ConsoleKey.X => AmigaKey.X,
            ConsoleKey.Y => AmigaKey.Y, ConsoleKey.Z => AmigaKey.Z,
            ConsoleKey.Enter => AmigaKey.Return, ConsoleKey.Escape => AmigaKey.Escape,
            ConsoleKey.Spacebar => AmigaKey.Space, ConsoleKey.Backspace => AmigaKey.Backspace,
            ConsoleKey.Tab => AmigaKey.Tab, ConsoleKey.Delete => AmigaKey.Delete,
            ConsoleKey.UpArrow => AmigaKey.Up, ConsoleKey.DownArrow => AmigaKey.Down,
            ConsoleKey.LeftArrow => AmigaKey.Left, ConsoleKey.RightArrow => AmigaKey.Right,
            ConsoleKey.F1 => AmigaKey.F1, ConsoleKey.F2 => AmigaKey.F2,
            ConsoleKey.F3 => AmigaKey.F3, ConsoleKey.F4 => AmigaKey.F4,
            ConsoleKey.F5 => AmigaKey.F5, ConsoleKey.F6 => AmigaKey.F6,
            ConsoleKey.F7 => AmigaKey.F7, ConsoleKey.F8 => AmigaKey.F8,
            ConsoleKey.F9 => AmigaKey.F9, ConsoleKey.F10 => AmigaKey.F10,
            _ => null
        };
    }
}
