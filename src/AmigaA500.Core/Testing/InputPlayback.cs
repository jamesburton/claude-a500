using AmigaA500.Core.Input;

namespace AmigaA500.Core.Testing;

/// <summary>
/// Pre-built input sequences for common test scenarios.
/// These simulate user interactions for automated testing.
/// </summary>
public static class InputPlayback
{
    /// <summary>
    /// Wait for title screen then press fire/enter to start.
    /// Common pattern: wait N frames, then press fire to dismiss title.
    /// </summary>
    public static SimulatedInput WaitAndStart(int waitFrames = 200)
    {
        return new SimulatedInput()
            .MouseClick(waitFrames, 0)         // Left click
            .KeyTap(waitFrames + 10, AmigaKey.Return)  // Enter
            .JoystickFire(waitFrames + 20, true)       // Fire button
            .JoystickFire(waitFrames + 25, false);
    }

    /// <summary>
    /// Navigate a menu: wait, then press down N times, then enter.
    /// </summary>
    public static SimulatedInput MenuSelect(int waitFrames, int downPresses, int delayBetween = 10)
    {
        var input = new SimulatedInput();
        long frame = waitFrames;

        for (int i = 0; i < downPresses; i++)
        {
            input.KeyTap(frame, AmigaKey.Down);
            frame += delayBetween;
        }

        input.KeyTap(frame, AmigaKey.Return);
        return input;
    }

    /// <summary>
    /// Type a string on the Amiga keyboard.
    /// </summary>
    public static SimulatedInput TypeString(long startFrame, string text, int charDelay = 5)
    {
        var input = new SimulatedInput();
        long frame = startFrame;

        foreach (char c in text)
        {
            var key = CharToAmigaKey(c);
            if (key.HasValue)
            {
                input.KeyTap(frame, key.Value);
                frame += charDelay;
            }
        }

        return input;
    }

    /// <summary>
    /// Simulate joystick waggle (rapid direction changes) — common for loading screens.
    /// </summary>
    public static SimulatedInput JoystickWaggle(long startFrame, int cycles = 10, int framesPer = 5)
    {
        var input = new SimulatedInput();
        long frame = startFrame;

        for (int i = 0; i < cycles; i++)
        {
            input.JoystickDirection(frame, false, false, true, false); // Left
            frame += framesPer;
            input.JoystickDirection(frame, false, false, false, true); // Right
            frame += framesPer;
        }

        input.JoystickDirection(frame, false, false, false, false); // Center
        return input;
    }

    /// <summary>
    /// Full game test sequence: wait for title, press start, wait for game, move around.
    /// </summary>
    public static SimulatedInput FullGameTest(int titleWait = 200, int gameplayFrames = 300)
    {
        var input = new SimulatedInput();

        // Wait for title screen
        input.MouseClick(titleWait, 0);
        input.KeyTap(titleWait + 5, AmigaKey.Space);

        // Gameplay: move joystick around, press fire
        long frame = titleWait + 50;
        input.JoystickDirection(frame, true, false, false, false); // Up
        frame += 30;
        input.JoystickFire(frame, true);
        frame += 10;
        input.JoystickFire(frame, false);
        frame += 20;
        input.JoystickDirection(frame, false, false, false, true); // Right
        frame += 30;
        input.JoystickDirection(frame, false, true, false, false); // Down
        frame += 30;
        input.JoystickFire(frame, true);
        frame += 5;
        input.JoystickFire(frame, false);
        frame += 20;
        input.JoystickDirection(frame, false, false, true, false); // Left
        frame += 30;
        input.JoystickDirection(frame, false, false, false, false); // Center

        return input;
    }

    private static AmigaKey? CharToAmigaKey(char c) => c switch
    {
        >= 'a' and <= 'z' => (AmigaKey)((int)AmigaKey.A + CharOffset(c)),
        >= 'A' and <= 'Z' => CharToAmigaKey(char.ToLower(c)),
        ' ' => AmigaKey.Space,
        '\n' => AmigaKey.Return,
        '0' => AmigaKey.Key0, '1' => AmigaKey.Key1, '2' => AmigaKey.Key2,
        '3' => AmigaKey.Key3, '4' => AmigaKey.Key4, '5' => AmigaKey.Key5,
        '6' => AmigaKey.Key6, '7' => AmigaKey.Key7, '8' => AmigaKey.Key8,
        '9' => AmigaKey.Key9,
        _ => null
    };

    // Amiga keys aren't sequential — map individually
    private static int CharOffset(char c) => c switch
    {
        'a' => 0, 'b' => (int)AmigaKey.B - (int)AmigaKey.A,
        'c' => (int)AmigaKey.C - (int)AmigaKey.A,
        _ => 0  // Simplified — full mapping in KeyboardMapper
    };
}
