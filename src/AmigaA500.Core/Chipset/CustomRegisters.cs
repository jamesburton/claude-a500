namespace AmigaA500.Core.Chipset;

/// <summary>
/// Custom chip register map ($DFF000-$DFF1FE).
/// Dispatches reads/writes to Agnus, Denise, Paula subsystems.
/// </summary>
public sealed class CustomRegisters : IChipRegisters
{
    // DMACON
    private ushort _dmacon;
    public ushort DMACON => _dmacon;

    // Interrupt control (Paula)
    private ushort _intena;
    private ushort _intreq;
    public ushort INTENA => _intena;
    public ushort INTREQ => _intreq;

    // Beam position (Agnus)
    public Func<ushort>? ReadVPOSR;
    public Func<ushort>? ReadVHPOSR;

    // Joystick/mouse
    public ushort JOY0DAT, JOY1DAT;

    // Collision
    public ushort CLXDAT;

    // ADKCON
    private ushort _adkcon;

    // Potentiometer
    public ushort POT0DAT, POT1DAT, POTGOR;

    // Serial
    public ushort SERDATR;

    // Color registers
    public readonly ushort[] Color = new ushort[32];

    // Bitplane control
    public ushort BPLCON0, BPLCON1, BPLCON2;
    public short BPL1MOD, BPL2MOD;

    // Bitplane pointers
    public readonly uint[] BplPt = new uint[6];

    // Sprite pointers
    public readonly uint[] SprPt = new uint[8];

    // Blitter
    public ushort BLTCON0, BLTCON1;
    public ushort BLTAFWM, BLTALWM;
    public uint BltAPt, BltBPt, BltCPt, BltDPt;
    public short BltAMod, BltBMod, BltCMod, BltDMod;
    public ushort BLTADAT, BLTBDAT, BLTCDAT;
    public Action<ushort>? OnBlitStart;

    // Copper
    public uint COP1LC, COP2LC;
    public Action? OnCopJmp1;
    public Action? OnCopJmp2;

    // Disk
    public uint DSKPT;
    public ushort DSKLEN;
    public ushort DSKSYNC;
    public Action<ushort>? OnDskLenWrite;

    // Audio
    public readonly uint[] AudLc = new uint[4];
    public readonly ushort[] AudLen = new ushort[4];
    public readonly ushort[] AudPer = new ushort[4];
    public readonly ushort[] AudVol = new ushort[4];
    public readonly ushort[] AudDat = new ushort[4];

    // Display window
    public ushort DIWSTRT, DIWSTOP;
    public ushort DDFSTRT, DDFSTOP;

    // Event callbacks
    public Action<int>? OnInterruptRequest;

    public ushort ReadRegister(uint offset)
    {
        return (offset & 0x1FE) switch
        {
            0x002 => (ushort)(_dmacon & 0x03FF), // DMACONR (no SET/CLR bit on read)
            0x004 => ReadVPOSR?.Invoke() ?? 0,
            0x006 => ReadVHPOSR?.Invoke() ?? 0,
            0x00A => JOY0DAT,
            0x00C => JOY1DAT,
            0x00E => ReadClxDat(),
            0x010 => (ushort)(_adkcon & 0x7FFF),
            0x012 => POT0DAT,
            0x014 => POT1DAT,
            0x016 => POTGOR,
            0x018 => SERDATR,
            0x01C => (ushort)(_intena & 0x7FFF),
            0x01E => (ushort)(_intreq & 0x7FFF),
            _ => 0xFFFF // Write-only registers
        };
    }

    public void WriteRegister(uint offset, ushort value)
    {
        switch (offset & 0x1FE)
        {
            // Disk
            case 0x020: DSKPT = (DSKPT & 0x0000FFFF) | ((uint)value << 16); break;
            case 0x022: DSKPT = (DSKPT & 0xFFFF0000) | value; break;
            case 0x024: DSKLEN = value; OnDskLenWrite?.Invoke(value); break;
            case 0x026: DSKSYNC = value; break;

            // Blitter
            case 0x040: BLTCON0 = value; break;
            case 0x042: BLTCON1 = value; break;
            case 0x044: BLTAFWM = value; break;
            case 0x046: BLTALWM = value; break;
            case 0x048: BltCPt = (BltCPt & 0xFFFF) | ((uint)value << 16); break;
            case 0x04A: BltCPt = (BltCPt & 0xFFFF0000) | value; break;
            case 0x04C: BltBPt = (BltBPt & 0xFFFF) | ((uint)value << 16); break;
            case 0x04E: BltBPt = (BltBPt & 0xFFFF0000) | value; break;
            case 0x050: BltAPt = (BltAPt & 0xFFFF) | ((uint)value << 16); break;
            case 0x052: BltAPt = (BltAPt & 0xFFFF0000) | value; break;
            case 0x054: BltDPt = (BltDPt & 0xFFFF) | ((uint)value << 16); break;
            case 0x056: BltDPt = (BltDPt & 0xFFFF0000) | value; break;
            case 0x058: OnBlitStart?.Invoke(value); break; // BLTSIZE — triggers blit
            case 0x060: BltAMod = (short)value; break;
            case 0x062: BltBMod = (short)value; break;
            case 0x064: BltCMod = (short)value; break;
            case 0x066: BltDMod = (short)value; break;

            // Copper
            case 0x080: COP1LC = (COP1LC & 0xFFFF) | ((uint)value << 16); break;
            case 0x082: COP1LC = (COP1LC & 0xFFFF0000) | value; break;
            case 0x084: COP2LC = (COP2LC & 0xFFFF) | ((uint)value << 16); break;
            case 0x086: COP2LC = (COP2LC & 0xFFFF0000) | value; break;
            case 0x088: OnCopJmp1?.Invoke(); break;
            case 0x08A: OnCopJmp2?.Invoke(); break;

            // DMA control
            case 0x096: WriteSetClr(ref _dmacon, value); break;

            // Interrupts
            case 0x09A: WriteSetClr(ref _intena, value); break;
            case 0x09C:
                WriteSetClr(ref _intreq, value);
                OnInterruptRequest?.Invoke(GetPendingInterruptLevel());
                break;

            // ADKCON
            case 0x09E: WriteSetClr(ref _adkcon, value); break;

            // Audio
            case 0x0A0: AudLc[0] = (AudLc[0] & 0xFFFF) | ((uint)value << 16); break;
            case 0x0A2: AudLc[0] = (AudLc[0] & 0xFFFF0000) | value; break;
            case 0x0A4: AudLen[0] = value; break;
            case 0x0A6: AudPer[0] = value; break;
            case 0x0A8: AudVol[0] = value; break;
            case 0x0AA: AudDat[0] = value; break;
            case 0x0B0: AudLc[1] = (AudLc[1] & 0xFFFF) | ((uint)value << 16); break;
            case 0x0B2: AudLc[1] = (AudLc[1] & 0xFFFF0000) | value; break;
            case 0x0B4: AudLen[1] = value; break;
            case 0x0B6: AudPer[1] = value; break;
            case 0x0B8: AudVol[1] = value; break;
            case 0x0BA: AudDat[1] = value; break;
            case 0x0C0: AudLc[2] = (AudLc[2] & 0xFFFF) | ((uint)value << 16); break;
            case 0x0C2: AudLc[2] = (AudLc[2] & 0xFFFF0000) | value; break;
            case 0x0C4: AudLen[2] = value; break;
            case 0x0C6: AudPer[2] = value; break;
            case 0x0C8: AudVol[2] = value; break;
            case 0x0CA: AudDat[2] = value; break;
            case 0x0D0: AudLc[3] = (AudLc[3] & 0xFFFF) | ((uint)value << 16); break;
            case 0x0D2: AudLc[3] = (AudLc[3] & 0xFFFF0000) | value; break;
            case 0x0D4: AudLen[3] = value; break;
            case 0x0D6: AudPer[3] = value; break;
            case 0x0D8: AudVol[3] = value; break;
            case 0x0DA: AudDat[3] = value; break;

            // Bitplane pointers
            case 0x0E0: BplPt[0] = (BplPt[0] & 0xFFFF) | ((uint)value << 16); break;
            case 0x0E2: BplPt[0] = (BplPt[0] & 0xFFFF0000) | value; break;
            case 0x0E4: BplPt[1] = (BplPt[1] & 0xFFFF) | ((uint)value << 16); break;
            case 0x0E6: BplPt[1] = (BplPt[1] & 0xFFFF0000) | value; break;
            case 0x0E8: BplPt[2] = (BplPt[2] & 0xFFFF) | ((uint)value << 16); break;
            case 0x0EA: BplPt[2] = (BplPt[2] & 0xFFFF0000) | value; break;
            case 0x0EC: BplPt[3] = (BplPt[3] & 0xFFFF) | ((uint)value << 16); break;
            case 0x0EE: BplPt[3] = (BplPt[3] & 0xFFFF0000) | value; break;
            case 0x0F0: BplPt[4] = (BplPt[4] & 0xFFFF) | ((uint)value << 16); break;
            case 0x0F2: BplPt[4] = (BplPt[4] & 0xFFFF0000) | value; break;
            case 0x0F4: BplPt[5] = (BplPt[5] & 0xFFFF) | ((uint)value << 16); break;
            case 0x0F6: BplPt[5] = (BplPt[5] & 0xFFFF0000) | value; break;

            // Bitplane control
            case 0x100: BPLCON0 = value; break;
            case 0x102: BPLCON1 = value; break;
            case 0x104: BPLCON2 = value; break;
            case 0x108: BPL1MOD = (short)value; break;
            case 0x10A: BPL2MOD = (short)value; break;

            // Display window
            case 0x08E: DIWSTRT = value; break;
            case 0x090: DIWSTOP = value; break;
            case 0x092: DDFSTRT = value; break;
            case 0x094: DDFSTOP = value; break;

            // Sprite pointers
            case >= 0x120 and <= 0x13E:
                int sprIdx = (int)(offset - 0x120) / 4;
                if ((offset & 2) == 0)
                    SprPt[sprIdx] = (SprPt[sprIdx] & 0xFFFF) | ((uint)value << 16);
                else
                    SprPt[sprIdx] = (SprPt[sprIdx] & 0xFFFF0000) | value;
                break;

            // Color registers
            case >= 0x180 and <= 0x1BE:
                Color[((int)offset - 0x180) / 2] = value;
                break;
        }
    }

    public int GetPendingInterruptLevel()
    {
        ushort active = (ushort)(_intena & _intreq & 0x3FFF);
        if ((_intena & 0x4000) == 0) return 0;

        if ((active & 0x2000) != 0) return 6;
        if ((active & 0x1800) != 0) return 5;
        if ((active & 0x0780) != 0) return 4;
        if ((active & 0x0070) != 0) return 3;
        if ((active & 0x0008) != 0) return 2;
        if ((active & 0x0007) != 0) return 1;
        return 0;
    }

    public void RequestInterrupt(int bit)
    {
        _intreq |= (ushort)(1 << bit);
        OnInterruptRequest?.Invoke(GetPendingInterruptLevel());
    }

    private ushort ReadClxDat()
    {
        ushort val = CLXDAT;
        CLXDAT = 0;
        return val;
    }

    private static void WriteSetClr(ref ushort register, ushort value)
    {
        if ((value & 0x8000) != 0)
            register |= (ushort)(value & 0x7FFF);
        else
            register &= (ushort)~(value & 0x7FFF);
    }
}
