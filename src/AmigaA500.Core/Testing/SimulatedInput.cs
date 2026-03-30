using AmigaA500.Core.Input;

namespace AmigaA500.Core.Testing;

/// <summary>
/// Simulated input playback for automated emulation testing.
/// Records and replays keyboard, mouse, and joystick input sequences
/// with frame-accurate timing for reproducible test runs.
/// </summary>
public sealed class SimulatedInput
{
    private readonly List<InputEvent> _events = new();
    private int _nextEventIndex;
    private long _currentFrame;

    public int EventCount => _events.Count;
    public long CurrentFrame => _currentFrame;

    /// <summary>
    /// Schedule a key press at a specific frame.
    /// </summary>
    public SimulatedInput KeyDown(long frame, AmigaKey key)
    {
        _events.Add(new InputEvent(frame, InputEventType.KeyDown, Key: key));
        return this;
    }

    /// <summary>
    /// Schedule a key release at a specific frame.
    /// </summary>
    public SimulatedInput KeyUp(long frame, AmigaKey key)
    {
        _events.Add(new InputEvent(frame, InputEventType.KeyUp, Key: key));
        return this;
    }

    /// <summary>
    /// Schedule a key press+release (tap) spanning a few frames.
    /// </summary>
    public SimulatedInput KeyTap(long frame, AmigaKey key, int holdFrames = 3)
    {
        KeyDown(frame, key);
        KeyUp(frame + holdFrames, key);
        return this;
    }

    /// <summary>
    /// Schedule mouse movement at a specific frame.
    /// </summary>
    public SimulatedInput MouseMove(long frame, int dx, int dy)
    {
        _events.Add(new InputEvent(frame, InputEventType.MouseMove, MouseX: dx, MouseY: dy));
        return this;
    }

    /// <summary>
    /// Schedule mouse button press.
    /// </summary>
    public SimulatedInput MouseDown(long frame, int button = 0)
    {
        _events.Add(new InputEvent(frame, InputEventType.MouseDown, MouseButton: button));
        return this;
    }

    public SimulatedInput MouseUp(long frame, int button = 0)
    {
        _events.Add(new InputEvent(frame, InputEventType.MouseUp, MouseButton: button));
        return this;
    }

    public SimulatedInput MouseClick(long frame, int button = 0, int holdFrames = 3)
    {
        MouseDown(frame, button);
        MouseUp(frame + holdFrames, button);
        return this;
    }

    /// <summary>
    /// Schedule joystick direction.
    /// </summary>
    public SimulatedInput JoystickDirection(long frame, bool up, bool down, bool left, bool right)
    {
        _events.Add(new InputEvent(frame, InputEventType.JoystickDir, JoyUp: up, JoyDown: down, JoyLeft: left, JoyRight: right));
        return this;
    }

    public SimulatedInput JoystickFire(long frame, bool pressed = true)
    {
        _events.Add(new InputEvent(frame, pressed ? InputEventType.JoyFireDown : InputEventType.JoyFireUp));
        return this;
    }

    /// <summary>
    /// Wait a number of frames (no input).
    /// </summary>
    public SimulatedInput Wait(int frames)
    {
        // No event needed — just advances the expected frame counter for chaining
        return this;
    }

    /// <summary>
    /// Sort events by frame and prepare for playback.
    /// </summary>
    public void Prepare()
    {
        _events.Sort((a, b) => a.Frame.CompareTo(b.Frame));
        _nextEventIndex = 0;
        _currentFrame = 0;
    }

    /// <summary>
    /// Get all events for the current frame and advance.
    /// Call once per emulated frame.
    /// </summary>
    public List<InputEvent> GetEventsForFrame(long frame)
    {
        _currentFrame = frame;
        var frameEvents = new List<InputEvent>();

        while (_nextEventIndex < _events.Count && _events[_nextEventIndex].Frame <= frame)
        {
            frameEvents.Add(_events[_nextEventIndex]);
            _nextEventIndex++;
        }

        return frameEvents;
    }

    public bool HasMoreEvents => _nextEventIndex < _events.Count;

    /// <summary>
    /// Apply events to the emulator's input devices.
    /// </summary>
    public static void ApplyEvents(List<InputEvent> events, Keyboard keyboard, Joystick joystick)
    {
        foreach (var evt in events)
        {
            switch (evt.Type)
            {
                case InputEventType.KeyDown:
                    if (evt.Key.HasValue) keyboard.KeyDown(evt.Key.Value);
                    break;
                case InputEventType.KeyUp:
                    if (evt.Key.HasValue) keyboard.KeyUp(evt.Key.Value);
                    break;
                case InputEventType.MouseMove:
                    joystick.MoveMouse(evt.MouseX, evt.MouseY);
                    break;
                case InputEventType.MouseDown:
                    joystick.Fire1 = true;
                    break;
                case InputEventType.MouseUp:
                    joystick.Fire1 = false;
                    break;
                case InputEventType.JoystickDir:
                    joystick.Up = evt.JoyUp;
                    joystick.Down = evt.JoyDown;
                    joystick.Left = evt.JoyLeft;
                    joystick.Right = evt.JoyRight;
                    break;
                case InputEventType.JoyFireDown:
                    joystick.Fire1 = true;
                    break;
                case InputEventType.JoyFireUp:
                    joystick.Fire1 = false;
                    break;
            }
        }
    }
}

public record InputEvent(
    long Frame,
    InputEventType Type,
    AmigaKey? Key = null,
    int MouseX = 0, int MouseY = 0, int MouseButton = 0,
    bool JoyUp = false, bool JoyDown = false, bool JoyLeft = false, bool JoyRight = false);

public enum InputEventType
{
    KeyDown, KeyUp,
    MouseMove, MouseDown, MouseUp,
    JoystickDir, JoyFireDown, JoyFireUp
}
