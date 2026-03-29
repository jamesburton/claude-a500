namespace AmigaA500.Core.Chipset;

/// <summary>
/// Real-Time Clock (RTC) — Oki MSM6242B compatible.
/// Mapped at $DC0000-$DC003F on the A500.
/// </summary>
public sealed class RealTimeClock
{
    private DateTime _baseTime;

    public RealTimeClock()
    {
        _baseTime = DateTime.Now;
    }

    public byte ReadRegister(int index)
    {
        var now = _baseTime;
        return (index & 0xF) switch
        {
            0x0 => (byte)(now.Second % 10),
            0x1 => (byte)(now.Second / 10),
            0x2 => (byte)(now.Minute % 10),
            0x3 => (byte)(now.Minute / 10),
            0x4 => (byte)(now.Hour % 10),
            0x5 => (byte)(now.Hour / 10),
            0x6 => (byte)(now.Day % 10),
            0x7 => (byte)(now.Day / 10),
            0x8 => (byte)(now.Month % 10),
            0x9 => (byte)(now.Month / 10),
            0xA => (byte)(now.Year % 10),
            0xB => (byte)((now.Year / 10) % 10),
            0xC => (byte)now.DayOfWeek,
            0xD => 0x04, // Control D: 24h mode
            0xE => 0x00, // Control E
            0xF => 0x00, // Control F
            _ => 0
        };
    }

    public void WriteRegister(int index, byte value)
    {
        // Write support for setting the clock (simplified — just store offsets)
    }
}
