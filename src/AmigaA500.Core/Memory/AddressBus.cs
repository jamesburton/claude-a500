namespace AmigaA500.Core.Memory;

/// <summary>
/// Amiga address bus — decodes 24-bit addresses to memory regions and I/O devices.
/// </summary>
public sealed class AddressBus : IBus
{
    private readonly byte[] _chipRam;
    private readonly byte[] _kickstartRom;
    private byte[]? _slowRam;

    private readonly IChipRegisters _customRegisters;
    private readonly ICiaAccess _ciaA;
    private readonly ICiaAccess _ciaB;

    private bool _overlay = true; // ROM overlay active at reset

    public AddressBus(
        int chipRamSize,
        byte[] kickstartRom,
        IChipRegisters customRegisters,
        ICiaAccess ciaA,
        ICiaAccess ciaB)
    {
        _chipRam = new byte[chipRamSize];
        _kickstartRom = kickstartRom;
        _customRegisters = customRegisters;
        _ciaA = ciaA;
        _ciaB = ciaB;
    }

    public bool Overlay
    {
        get => _overlay;
        set => _overlay = value;
    }

    public void EnableSlowRam(int size = 512 * 1024)
    {
        _slowRam = new byte[size];
    }

    public byte ReadByte(uint address)
    {
        address &= 0xFFFFFF;

        // Chip RAM or ROM overlay
        if (address < 0x080000)
        {
            if (_overlay)
                return ReadRomByte(address + 0xFC0000);
            return _chipRam[address];
        }

        // Slow RAM
        if (address >= 0xC00000 && address < 0xC80000 && _slowRam != null)
            return _slowRam[address - 0xC00000];

        // CIA space
        if (address >= 0xBF0000 && address < 0xC00000)
            return ReadCiaByte(address);

        // Custom chip registers (byte read returns high or low byte of word)
        if (address >= 0xDFF000 && address < 0xE00000)
        {
            uint regOffset = (address - 0xDFF000) & 0x1FE;
            ushort word = _customRegisters.ReadRegister(regOffset);
            return (address & 1) == 0 ? (byte)(word >> 8) : (byte)(word & 0xFF);
        }

        // Kickstart ROM
        if (address >= 0xFC0000)
            return ReadRomByte(address);

        return 0xFF; // Open bus
    }

    public void WriteByte(uint address, byte value)
    {
        address &= 0xFFFFFF;

        if (address < 0x080000 && !_overlay)
        {
            _chipRam[address] = value;
            return;
        }

        if (address >= 0xC00000 && address < 0xC80000 && _slowRam != null)
        {
            _slowRam[address - 0xC00000] = value;
            return;
        }

        if (address >= 0xBF0000 && address < 0xC00000)
        {
            WriteCiaByte(address, value);
            return;
        }

        if (address >= 0xDFF000 && address < 0xE00000)
        {
            // Byte writes to custom registers: accumulate high/low then write word
            // Most software uses word writes; byte writes are rare
            return;
        }

        // ROM writes ignored
    }

    public ushort ReadWord(uint address)
    {
        address &= 0xFFFFFE; // Force word alignment
        return (ushort)(ReadByte(address) << 8 | ReadByte(address + 1));
    }

    public void WriteWord(uint address, ushort value)
    {
        address &= 0xFFFFFE;

        // Fast path for chip RAM
        if (address < 0x080000 && !_overlay)
        {
            _chipRam[address] = (byte)(value >> 8);
            _chipRam[address + 1] = (byte)(value & 0xFF);
            return;
        }

        // Custom chip registers (direct word write)
        if (address >= 0xDFF000 && address < 0xE00000)
        {
            uint regOffset = (address - 0xDFF000) & 0x1FE;
            _customRegisters.WriteRegister(regOffset, value);
            return;
        }

        WriteByte(address, (byte)(value >> 8));
        WriteByte(address + 1, (byte)(value & 0xFF));
    }

    // DMA access — direct chip RAM, bypasses address decoder
    public ushort DmaReadWord(uint address)
    {
        address &= (uint)(_chipRam.Length - 1);
        return (ushort)(_chipRam[address] << 8 | _chipRam[address + 1]);
    }

    public void DmaWriteWord(uint address, ushort value)
    {
        address &= (uint)(_chipRam.Length - 1);
        _chipRam[address] = (byte)(value >> 8);
        _chipRam[address + 1] = (byte)(value & 0xFF);
    }

    private byte ReadRomByte(uint address)
    {
        uint romOffset = (address - 0xFC0000) % (uint)_kickstartRom.Length;
        return _kickstartRom[romOffset];
    }

    private byte ReadCiaByte(uint address)
    {
        // CIA-A: odd addresses ($BFE001, $BFE101, ...)
        // CIA-B: even addresses ($BFD000, $BFD100, ...)
        int regIndex = (int)((address >> 8) & 0xF);

        if ((address & 1) != 0 && address >= 0xBFE000)
            return _ciaA.ReadRegister(regIndex);
        if ((address & 1) == 0 && address >= 0xBFD000 && address < 0xBFE000)
            return _ciaB.ReadRegister(regIndex);

        return 0xFF;
    }

    private void WriteCiaByte(uint address, byte value)
    {
        int regIndex = (int)((address >> 8) & 0xF);

        if ((address & 1) != 0 && address >= 0xBFE000)
            _ciaA.WriteRegister(regIndex, value);
        else if ((address & 1) == 0 && address >= 0xBFD000 && address < 0xBFE000)
            _ciaB.WriteRegister(regIndex, value);
    }
}
