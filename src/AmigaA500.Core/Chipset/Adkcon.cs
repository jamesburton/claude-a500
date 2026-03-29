namespace AmigaA500.Core.Chipset;

/// <summary>
/// ADKCON — Audio and Disk Control Register.
/// Controls audio modulation, disk precompensation, and MFM/GCR mode.
/// </summary>
public sealed class Adkcon
{
    private ushort _value;

    public ushort Value => _value;

    public void Write(ushort data)
    {
        if ((data & 0x8000) != 0)
            _value |= (ushort)(data & 0x7FFF);
        else
            _value &= (ushort)~(data & 0x7FFF);
    }

    // Audio modulation flags
    public bool UseAud0Modulation => (_value & 0x0001) != 0; // AUD0 period from AUD1
    public bool UseAud1Modulation => (_value & 0x0002) != 0; // AUD1 volume from AUD0
    public bool UseAud2Modulation => (_value & 0x0004) != 0; // AUD2 period from AUD3
    public bool UseAud3Modulation => (_value & 0x0008) != 0; // AUD3 volume from AUD2

    // Disk flags
    public bool FastDisk => (_value & 0x0100) != 0;    // Fast disk access
    public bool MfmMode => (_value & 0x0200) == 0;     // 0=MFM (default), 1=GCR
    public bool WordSync => (_value & 0x0400) != 0;     // Word sync enable
    public bool MfmSync => (_value & 0x0800) != 0;      // MFM sync enable

    // Precompensation
    public int Precomp => (_value >> 12) & 3;
}
