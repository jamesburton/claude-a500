using AmigaA500.Core.Cia;

namespace AmigaA500.Tests.Chipset;

public class CiaTests
{
    [Fact]
    public void TimerA_Countdown()
    {
        var cia = new Cia8520();
        cia.WriteRegister(0x4, 0x0A); // Timer A low = 10
        cia.WriteRegister(0x5, 0x00); // Timer A high = 0
        cia.WriteRegister(0xE, 0x01); // Start Timer A

        for (int i = 0; i < 9; i++) cia.Tick();

        byte lo = cia.ReadRegister(0x4);
        Assert.Equal(1, lo);
    }

    [Fact]
    public void TimerA_Underflow_TriggersInterrupt()
    {
        var cia = new Cia8520();
        bool interrupted = false;
        cia.OnInterrupt = _ => interrupted = true;

        cia.WriteRegister(0xD, 0x81); // Enable Timer A interrupt
        cia.WriteRegister(0x4, 0x03); // Timer A = 3
        cia.WriteRegister(0x5, 0x00);
        cia.WriteRegister(0xE, 0x01); // Start

        cia.Tick(); cia.Tick(); cia.Tick();
        Assert.True(interrupted);
    }

    [Fact]
    public void TimerA_OneShot_Stops()
    {
        var cia = new Cia8520();
        cia.WriteRegister(0x4, 0x02);
        cia.WriteRegister(0x5, 0x00);
        cia.WriteRegister(0xE, 0x09); // Start + one-shot

        cia.Tick(); cia.Tick(); // Timer fires
        Assert.Equal(0, cia.CRA & 0x01); // Timer stopped
    }

    [Fact]
    public void ICR_ReadAndClear()
    {
        var cia = new Cia8520();
        cia.WriteRegister(0xD, 0x81); // Enable Timer A interrupt
        cia.WriteRegister(0x4, 0x01);
        cia.WriteRegister(0x5, 0x00);
        cia.WriteRegister(0xE, 0x01);
        cia.Tick(); // Underflow

        byte icr = cia.ReadRegister(0xD);
        Assert.True((icr & 0x01) != 0); // Timer A flag set
        Assert.True((icr & 0x80) != 0); // IR flag set

        byte icr2 = cia.ReadRegister(0xD);
        Assert.Equal(0, icr2); // Cleared after read
    }

    [Fact]
    public void Port_DataDirection()
    {
        var cia = new Cia8520();
        cia.ReadPortAExternal = () => 0xAA; // External inputs
        cia.WriteRegister(0x2, 0xF0); // DDRA: high nibble output
        cia.WriteRegister(0x0, 0xFF); // PRA: write $FF

        byte val = cia.ReadRegister(0x0);
        // High nibble: from PRA (0xF0), low nibble: from external (0xA)
        Assert.Equal(0xFA, val);
    }

    [Fact]
    public void TOD_CountsUp()
    {
        var cia = new Cia8520();
        cia.TickTod();
        cia.TickTod();
        cia.TickTod();

        byte lo = cia.ReadRegister(0x8);
        Assert.Equal(3, lo);
    }

    [Fact]
    public void TOD_Alarm_TriggersInterrupt()
    {
        var cia = new Cia8520();
        bool interrupted = false;
        cia.OnInterrupt = _ => interrupted = true;

        cia.WriteRegister(0xD, 0x84); // Enable TOD alarm interrupt
        cia.WriteRegister(0xF, 0x80); // CRB bit 7: write to alarm
        cia.WriteRegister(0x8, 0x05); // Alarm at count 5
        cia.WriteRegister(0xF, 0x00); // Back to normal

        for (int i = 0; i < 5; i++) cia.TickTod();
        Assert.True(interrupted);
    }
}
