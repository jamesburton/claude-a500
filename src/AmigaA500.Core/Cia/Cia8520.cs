namespace AmigaA500.Core.Cia;

/// <summary>
/// MOS 8520 Complex Interface Adapter emulation.
/// </summary>
public class Cia8520 : ICiaAccess
{
    // Port registers
    public byte PRA, PRB;
    public byte DDRA, DDRB;

    // Timers
    private ushort _timerALatch, _timerBLatch;
    private ushort _timerACounter, _timerBCounter;
    public byte CRA, CRB;

    // TOD
    private uint _tod;
    private uint _todAlarm;
    private uint _todLatch;
    private bool _todLatched;

    // Serial
    public byte SDR;

    // Interrupts
    private byte _icrMask;
    private byte _icrData;

    // External input providers
    public Func<byte>? ReadPortAExternal;
    public Func<byte>? ReadPortBExternal;
    public Action<byte>? OnInterrupt;

    public byte ReadRegister(int index)
    {
        return index switch
        {
            0x0 => ReadPortA(),
            0x1 => ReadPortB(),
            0x2 => DDRA,
            0x3 => DDRB,
            0x4 => (byte)(_timerACounter & 0xFF),
            0x5 => (byte)(_timerACounter >> 8),
            0x6 => (byte)(_timerBCounter & 0xFF),
            0x7 => (byte)(_timerBCounter >> 8),
            0x8 => ReadTodLow(),
            0x9 => (byte)((_todLatched ? _todLatch : _tod) >> 8),
            0xA => ReadTodHigh(),
            0xC => SDR,
            0xD => ReadICR(),
            0xE => CRA,
            0xF => CRB,
            _ => 0xFF
        };
    }

    public void WriteRegister(int index, byte value)
    {
        switch (index)
        {
            case 0x0: PRA = value; break;
            case 0x1: PRB = value; break;
            case 0x2: DDRA = value; break;
            case 0x3: DDRB = value; break;
            case 0x4: _timerALatch = (ushort)((_timerALatch & 0xFF00) | value); break;
            case 0x5:
                _timerALatch = (ushort)((_timerALatch & 0x00FF) | (value << 8));
                if ((CRA & 0x01) == 0) _timerACounter = _timerALatch; // Load if stopped
                break;
            case 0x6: _timerBLatch = (ushort)((_timerBLatch & 0xFF00) | value); break;
            case 0x7:
                _timerBLatch = (ushort)((_timerBLatch & 0x00FF) | (value << 8));
                if ((CRB & 0x01) == 0) _timerBCounter = _timerBLatch;
                break;
            case 0x8:
                if ((CRB & 0x80) != 0) _todAlarm = (_todAlarm & 0xFFFF00) | value;
                else { _tod = (_tod & 0xFFFF00) | value; _todLatched = false; }
                break;
            case 0x9:
                if ((CRB & 0x80) != 0) _todAlarm = (_todAlarm & 0xFF00FF) | ((uint)value << 8);
                else _tod = (_tod & 0xFF00FF) | ((uint)value << 8);
                break;
            case 0xA:
                if ((CRB & 0x80) != 0) _todAlarm = (_todAlarm & 0x00FFFF) | ((uint)value << 16);
                else _tod = (_tod & 0x00FFFF) | ((uint)value << 16);
                break;
            case 0xC: SDR = value; break;
            case 0xD: WriteICR(value); break;
            case 0xE:
                CRA = value;
                if ((value & 0x10) != 0) { _timerACounter = _timerALatch; CRA &= 0xEF; }
                break;
            case 0xF:
                CRB = value;
                if ((value & 0x10) != 0) { _timerBCounter = _timerBLatch; CRB &= 0xEF; }
                break;
        }
    }

    public void Tick()
    {
        // Timer A
        if ((CRA & 0x01) != 0 && (CRA & 0x20) == 0) // Running, counting phi2
        {
            if (--_timerACounter == 0)
            {
                _timerACounter = _timerALatch;
                SetIcr(0x01);
                if ((CRA & 0x08) != 0) CRA &= 0xFE; // One-shot: stop

                // Timer B counting Timer A underflows
                if ((CRB & 0x01) != 0 && (CRB & 0x60) == 0x40)
                    TickTimerB();
            }
        }

        // Timer B (phi2 mode)
        if ((CRB & 0x01) != 0 && (CRB & 0x60) == 0x00)
            TickTimerB();
    }

    private void TickTimerB()
    {
        if (--_timerBCounter == 0)
        {
            _timerBCounter = _timerBLatch;
            SetIcr(0x02);
            if ((CRB & 0x08) != 0) CRB &= 0xFE;
        }
    }

    public void TickTod()
    {
        _tod++;
        if ((_tod & 0xFFFFFF) == (_todAlarm & 0xFFFFFF))
            SetIcr(0x04);
    }

    public void TriggerSerialInterrupt() => SetIcr(0x08);
    public void TriggerFlagInterrupt() => SetIcr(0x10);

    private byte ReadPortA()
    {
        byte ext = ReadPortAExternal?.Invoke() ?? 0xFF;
        return (byte)((PRA & DDRA) | (ext & ~DDRA));
    }

    private byte ReadPortB()
    {
        byte ext = ReadPortBExternal?.Invoke() ?? 0xFF;
        return (byte)((PRB & DDRB) | (ext & ~DDRB));
    }

    private byte ReadTodLow()
    {
        byte val = (byte)(_todLatched ? _todLatch : _tod);
        _todLatched = false;
        return val;
    }

    private byte ReadTodHigh()
    {
        _todLatch = _tod;
        _todLatched = true;
        return (byte)(_todLatch >> 16);
    }

    private void SetIcr(byte flags)
    {
        _icrData |= flags;
        if ((_icrData & _icrMask) != 0)
        {
            _icrData |= 0x80;
            OnInterrupt?.Invoke(_icrData);
        }
    }

    private byte ReadICR()
    {
        byte result = _icrData;
        _icrData = 0;
        return result;
    }

    private void WriteICR(byte value)
    {
        if ((value & 0x80) != 0)
            _icrMask |= (byte)(value & 0x7F);
        else
            _icrMask &= (byte)~(value & 0x7F);

        // Check if pending interrupt should now fire
        if ((_icrData & _icrMask & 0x1F) != 0)
        {
            _icrData |= 0x80;
            OnInterrupt?.Invoke(_icrData);
        }
    }
}
