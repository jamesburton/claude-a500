namespace AmigaA500.Core.Cpu;

/// <summary>
/// Motorola 68000 CPU emulation core.
/// </summary>
public sealed partial class Mc68000
{
    // Data registers D0-D7
    public readonly uint[] D = new uint[8];

    // Address registers A0-A7 (A7 is the active stack pointer)
    public readonly uint[] A = new uint[8];

    // Program counter
    public uint PC;

    // Status register
    private ushort _sr;

    // Saved stack pointers for mode switching
    private uint _usp; // User stack pointer
    private uint _ssp; // Supervisor stack pointer

    // Prefetch queue
    internal ushort IRC; // Instruction Register Capture (prefetched word)
    internal ushort IRD; // Instruction Register Decode (current opcode)

    // Cycle counter for current instruction
    public int Cycles;

    // Total cycles executed
    public long TotalCycles;

    // Halted state (STOP instruction)
    public bool Halted;

    // Bus interface
    private readonly IBus _bus;

    public Mc68000(IBus bus)
    {
        _bus = bus;
        _sr = 0x2700; // Supervisor mode, all interrupts masked
    }

    #region Status Register

    public ushort SR
    {
        get => _sr;
        set
        {
            bool wasSupervisor = Supervisor;
            _sr = value;
            if (wasSupervisor != Supervisor)
                SwapStackPointers();
        }
    }

    // Condition Code Register (low byte of SR)
    public byte CCR
    {
        get => (byte)(_sr & 0xFF);
        set => _sr = (ushort)((_sr & 0xFF00) | value);
    }

    // Individual flags
    public bool C { get => (_sr & 0x01) != 0; set => _sr = value ? (ushort)(_sr | 0x01) : (ushort)(_sr & ~0x01); }
    public bool V { get => (_sr & 0x02) != 0; set => _sr = value ? (ushort)(_sr | 0x02) : (ushort)(_sr & ~0x02); }
    public bool Z { get => (_sr & 0x04) != 0; set => _sr = value ? (ushort)(_sr | 0x04) : (ushort)(_sr & ~0x04); }
    public bool N { get => (_sr & 0x08) != 0; set => _sr = value ? (ushort)(_sr | 0x08) : (ushort)(_sr & ~0x08); }
    public bool X { get => (_sr & 0x10) != 0; set => _sr = value ? (ushort)(_sr | 0x10) : (ushort)(_sr & ~0x10); }

    // System byte
    public int InterruptMask
    {
        get => (_sr >> 8) & 7;
        set => _sr = (ushort)((_sr & 0xF8FF) | ((value & 7) << 8));
    }

    public bool Supervisor
    {
        get => (_sr & 0x2000) != 0;
        set => SR = value ? (ushort)(_sr | 0x2000) : (ushort)(_sr & ~0x2000);
    }

    public bool Trace
    {
        get => (_sr & 0x8000) != 0;
        set => _sr = value ? (ushort)(_sr | 0x8000) : (ushort)(_sr & ~0x8000);
    }

    private void SwapStackPointers()
    {
        if (Supervisor)
        {
            // Entering supervisor mode: save USP, restore SSP
            _usp = A[7];
            A[7] = _ssp;
        }
        else
        {
            // Entering user mode: save SSP, restore USP
            _ssp = A[7];
            A[7] = _usp;
        }
    }

    #endregion

    #region Bus Access

    internal ushort ReadWord(uint address)
    {
        Cycles += 4;
        return _bus.ReadWord(address & 0xFFFFFE); // Force word alignment
    }

    internal void WriteWord(uint address, ushort value)
    {
        Cycles += 4;
        _bus.WriteWord(address & 0xFFFFFE, value);
    }

    internal byte ReadByte(uint address)
    {
        Cycles += 4;
        return _bus.ReadByte(address);
    }

    internal void WriteByte(uint address, byte value)
    {
        Cycles += 4;
        _bus.WriteByte(address, value);
    }

    internal uint ReadLong(uint address)
    {
        uint hi = ReadWord(address);
        uint lo = ReadWord(address + 2);
        return (hi << 16) | lo;
    }

    internal void WriteLong(uint address, uint value)
    {
        WriteWord(address, (ushort)(value >> 16));
        WriteWord(address + 2, (ushort)value);
    }

    internal ushort FetchWord()
    {
        ushort word = ReadWord(PC);
        PC += 2;
        return word;
    }

    internal uint FetchLong()
    {
        uint value = ReadLong(PC);
        PC += 4;
        return value;
    }

    #endregion

    #region Reset

    public void Reset()
    {
        _sr = 0x2700; // Supervisor mode, interrupts masked
        _ssp = (uint)(_bus.ReadWord(0) << 16) | _bus.ReadWord(2);
        A[7] = _ssp;
        PC = (uint)(_bus.ReadWord(4) << 16 | _bus.ReadWord(6));
        Halted = false;
        Cycles = 0;

        // Initial prefetch
        IRC = ReadWord(PC);
    }

    #endregion

    #region Exception Processing

    public void RaiseException(int vector)
    {
        ushort savedSR = _sr;

        // Enter supervisor mode, clear trace
        if (!Supervisor)
        {
            _usp = A[7];
            _sr |= 0x2000;
            A[7] = _ssp;
        }
        _sr &= unchecked((ushort)~0x8000); // Clear trace

        // Push PC and SR to supervisor stack
        A[7] -= 4;
        WriteLong(A[7], PC);
        A[7] -= 2;
        WriteWord(A[7], savedSR);

        // Read vector and jump
        PC = ReadLong((uint)(vector * 4));
        Cycles += 4; // Internal processing
    }

    public void RaiseInterrupt(int level)
    {
        if (level > InterruptMask || level == 7)
        {
            ushort savedSR = _sr;

            if (!Supervisor)
            {
                _usp = A[7];
                _sr |= 0x2000;
                A[7] = _ssp;
            }
            _sr &= unchecked((ushort)~0x8000);
            InterruptMask = level;

            A[7] -= 4;
            WriteLong(A[7], PC);
            A[7] -= 2;
            WriteWord(A[7], savedSR);

            // Auto-vector: vector 25-31 for levels 1-7
            PC = ReadLong((uint)((24 + level) * 4));
            Halted = false;
            Cycles += 4;
        }
    }

    #endregion
}
