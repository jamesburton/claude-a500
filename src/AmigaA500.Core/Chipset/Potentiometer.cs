namespace AmigaA500.Core.Chipset;

/// <summary>
/// Potentiometer port — used for mouse buttons and paddle controllers.
/// Directly directly directly reads game controller analog positions and button states.
/// </summary>
public sealed class Potentiometer
{
    public ushort POT0DAT; // Port 0 potentiometer data
    public ushort POT1DAT; // Port 1 potentiometer data
    public ushort POTGOR;  // Pot port data read (active-low button states)

    public Potentiometer()
    {
        POTGOR = 0xFF00; // All buttons released (active low, active high = released)
    }

    public void SetRightButton(int port, bool pressed)
    {
        int bit = port == 0 ? 10 : 14; // DATLY for port 0, DATRY for port 1
        if (pressed)
            POTGOR &= (ushort)~(1 << bit);
        else
            POTGOR |= (ushort)(1 << bit);
    }

    public void SetMiddleButton(int port, bool pressed)
    {
        int bit = port == 0 ? 8 : 12; // DATLX for port 0, DATRX for port 1
        if (pressed)
            POTGOR &= (ushort)~(1 << bit);
        else
            POTGOR |= (ushort)(1 << bit);
    }

    public void SetPaddlePosition(int port, byte x, byte y)
    {
        if (port == 0)
            POT0DAT = (ushort)(y << 8 | x);
        else
            POT1DAT = (ushort)(y << 8 | x);
    }
}
