using AmigaA500.Core;
using AmigaA500.Core.Cpu;
using AmigaA500.Core.Input;
using AmigaA500.Core.Testing;

namespace AmigaA500.Tests.Testing;

public class DeepEmulationTests
{
    private static byte[] CreateTestRom(params ushort[] extraCode)
    {
        var rom = new byte[256 * 1024];
        rom[0] = 0x00; rom[1] = 0x00; rom[2] = 0xFF; rom[3] = 0x00;
        rom[4] = 0x00; rom[5] = 0xFC; rom[6] = 0x00; rom[7] = 0x08;
        for (int i = 0; i < extraCode.Length; i++)
        {
            rom[8 + i * 2] = (byte)(extraCode[i] >> 8);
            rom[9 + i * 2] = (byte)(extraCode[i] & 0xFF);
        }
        return rom;
    }

    [Fact]
    public void SimulatedInput_SchedulesEvents()
    {
        var input = new SimulatedInput()
            .KeyDown(10, AmigaKey.Space)
            .KeyUp(15, AmigaKey.Space)
            .MouseClick(20);

        input.Prepare();
        Assert.True(input.EventCount >= 3);
    }

    [Fact]
    public void SimulatedInput_ReturnsEventsInOrder()
    {
        var input = new SimulatedInput()
            .KeyDown(5, AmigaKey.A)
            .KeyDown(10, AmigaKey.B)
            .KeyDown(3, AmigaKey.C); // Out of order

        input.Prepare();

        var frame3 = input.GetEventsForFrame(3);
        Assert.Single(frame3);
        Assert.Equal(AmigaKey.C, frame3[0].Key);

        var frame5 = input.GetEventsForFrame(5);
        Assert.Single(frame5);
        Assert.Equal(AmigaKey.A, frame5[0].Key);
    }

    [Fact]
    public void SimulatedInput_ApplyEvents_UpdatesKeyboard()
    {
        var kb = new Keyboard();
        var joy = new Joystick();

        var events = new List<InputEvent>
        {
            new(0, InputEventType.KeyDown, Key: AmigaKey.Return)
        };

        SimulatedInput.ApplyEvents(events, kb, joy);
        Assert.True(kb.HasData);
    }

    [Fact]
    public void SimulatedInput_ApplyEvents_UpdatesJoystick()
    {
        var kb = new Keyboard();
        var joy = new Joystick();

        var events = new List<InputEvent>
        {
            new(0, InputEventType.JoystickDir, JoyUp: true, JoyRight: true),
            new(0, InputEventType.JoyFireDown)
        };

        SimulatedInput.ApplyEvents(events, kb, joy);
        Assert.True(joy.Up);
        Assert.True(joy.Right);
        Assert.True(joy.Fire1);
    }

    [Fact]
    public void AcceleratedRunner_RunsFrames()
    {
        var rom = CreateTestRom(0x4E71, 0x4E71); // NOP NOP
        var amiga = new Amiga(rom);
        amiga.Reset();

        var runner = new AcceleratedRunner(amiga);
        runner.RunFrames(10);

        Assert.Equal(10, runner.FramesExecuted);
        Assert.True(runner.TotalCpuCycles > 0);
    }

    [Fact]
    public void AcceleratedRunner_WithInput()
    {
        var rom = CreateTestRom(0x4E71);
        var amiga = new Amiga(rom);
        amiga.Reset();

        var input = new SimulatedInput()
            .KeyTap(5, AmigaKey.Space);

        var runner = new AcceleratedRunner(amiga, input);
        runner.RunFrames(10);

        Assert.Equal(10, runner.FramesExecuted);
    }

    [Fact]
    public void AcceleratedRunner_RunUntilCondition()
    {
        var rom = CreateTestRom(0x4E71);
        var amiga = new Amiga(rom);
        amiga.Reset();

        var runner = new AcceleratedRunner(amiga);
        bool result = runner.RunUntil(() => runner.FramesExecuted >= 5, maxFrames: 100);

        Assert.True(result);
        Assert.Equal(5, runner.FramesExecuted);
    }

    [Fact]
    public void AcceleratedRunner_Timeout()
    {
        var rom = CreateTestRom(0x4E71);
        var amiga = new Amiga(rom);
        amiga.Reset();

        var runner = new AcceleratedRunner(amiga);
        bool result = runner.RunUntil(() => false, maxFrames: 10);

        Assert.False(result); // Timed out
        Assert.Equal(10, runner.FramesExecuted);
    }

    [Fact]
    public void InputPlayback_WaitAndStart()
    {
        var input = InputPlayback.WaitAndStart(100);
        input.Prepare();
        Assert.True(input.EventCount >= 4);
    }

    [Fact]
    public void InputPlayback_FullGameTest()
    {
        var input = InputPlayback.FullGameTest();
        input.Prepare();
        Assert.True(input.EventCount >= 8);
    }

    [Fact]
    public void InputPlayback_MenuSelect()
    {
        var input = InputPlayback.MenuSelect(50, 3);
        input.Prepare();
        // 3 down presses + 1 enter = at least 4 events (each tap = 2 events)
        Assert.True(input.EventCount >= 4);
    }

    [Fact]
    public void AcceleratedRunner_CaptureTitleScreen()
    {
        var rom = CreateTestRom(0x4E71);
        var amiga = new Amiga(rom);
        amiga.Reset();
        // Put something in the framebuffer so it's not blank
        amiga.Framebuffer[0] = 0xFFFF0000;
        amiga.Framebuffer[100] = 0xFF00FF00;
        amiga.Framebuffer[1000] = 0xFF0000FF;
        amiga.Framebuffer[5000] = 0xFFFFFFFF;

        var runner = new AcceleratedRunner(amiga);
        var dir = Path.Combine(Path.GetTempPath(), "amiga-test-screens");

        string? path = runner.CaptureTitleScreen(dir, "TestGame");
        Assert.NotNull(path);
        Assert.True(File.Exists(path));

        // Cleanup
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
    }
}
