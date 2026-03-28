using AmigaA500.Core.Input;

namespace AmigaA500.Tests.Input;

public class InputTests
{
    [Fact]
    public void Keyboard_EncodesDecode()
    {
        var kb = new Keyboard();
        kb.KeyDown(AmigaKey.A);

        Assert.True(kb.HasData);
        byte raw = kb.ReadData();

        var (scanCode, released) = Keyboard.DecodeRawData(raw);
        Assert.Equal((byte)AmigaKey.A, scanCode);
        Assert.False(released);
    }

    [Fact]
    public void Keyboard_ReleaseEvent()
    {
        var kb = new Keyboard();
        kb.KeyUp(AmigaKey.Space);

        byte raw = kb.ReadData();
        var (scanCode, released) = Keyboard.DecodeRawData(raw);
        Assert.Equal((byte)AmigaKey.Space, scanCode);
        Assert.True(released);
    }

    [Fact]
    public void Keyboard_BuffersMultipleKeys()
    {
        var kb = new Keyboard();
        kb.KeyDown(AmigaKey.A);
        kb.KeyDown(AmigaKey.B);
        kb.KeyUp(AmigaKey.A);

        Assert.True(kb.HasData);
        kb.ReadData(); // A down
        kb.ReadData(); // B down
        kb.ReadData(); // A up
        Assert.False(kb.HasData);
    }

    [Fact]
    public void Keyboard_OnKeyReadyCallback()
    {
        int callCount = 0;
        var kb = new Keyboard(() => callCount++);
        kb.KeyDown(AmigaKey.Return);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void Joystick_UpDirection()
    {
        var js = new Joystick { Up = true };
        ushort dat = js.ReadJoyDat();
        // Up: V1=0, V0=1 (XOR of Up and Down)
        Assert.Equal(1, (dat >> 8) & 1); // V0 = 1
        Assert.Equal(0, (dat >> 9) & 1); // V1 = 0
    }

    [Fact]
    public void Joystick_DownDirection()
    {
        var js = new Joystick { Down = true };
        ushort dat = js.ReadJoyDat();
        Assert.Equal(1, (dat >> 8) & 1); // V0 = XOR(down,up) = 1
        Assert.Equal(1, (dat >> 9) & 1); // V1 = down = 1
    }

    [Fact]
    public void Joystick_RightDirection()
    {
        var js = new Joystick { Right = true };
        ushort dat = js.ReadJoyDat();
        Assert.Equal(1, dat & 1);        // H0 = XOR(right,left) = 1
        Assert.Equal(1, (dat >> 1) & 1); // H1 = right = 1
    }

    [Fact]
    public void Mouse_Counters()
    {
        var mouse = new Joystick();
        mouse.MoveMouse(10, -5);
        ushort dat = mouse.ReadMouseDat();
        Assert.Equal(10, dat & 0xFF);          // X counter
        Assert.Equal(unchecked((byte)(-5)), (dat >> 8) & 0xFF); // Y counter (wraps)
    }
}
