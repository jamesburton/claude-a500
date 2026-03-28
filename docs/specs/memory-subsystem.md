# Technical Spec: Memory Subsystem

## Address Decoder

### Memory Map Implementation

```csharp
class AddressBus
{
    byte[] ChipRam;        // 512 KB default ($000000-$07FFFF)
    byte[] KickstartRom;   // 256 KB ($FC0000-$FFFFFF)
    byte[] SlowRam;        // Optional 512 KB ($C00000-$C7FFFF)
    CustomChipRegisters Custom;  // $DFF000-$DFF1FE
    Cia CiaA, CiaB;       // $BFE001 (odd), $BFD000 (even)
    bool OVL;              // Kickstart overlay at $000000

    // Decode address to memory region
    ushort ReadWord(uint addr)
    {
        addr &= 0xFFFFFF; // 24-bit address space

        return addr switch
        {
            // Chip RAM (or ROM overlay)
            < 0x080000 when OVL => ReadRomWord(addr + 0xFC0000),
            < 0x080000 => ReadChipWord(addr),

            // Slow RAM (if present)
            >= 0xC00000 and < 0xC80000 when SlowRam != null => ReadSlowWord(addr),

            // CIA space
            >= 0xBF0000 and < 0xC00000 => ReadCIA(addr),

            // Custom chip registers
            >= 0xDFF000 and < 0xE00000 => Custom.ReadRegister((addr - 0xDFF000) & 0x1FE),

            // Kickstart ROM
            >= 0xFC0000 => ReadRomWord(addr),

            // Unmapped: return open bus
            _ => 0xFFFF
        };
    }

    void WriteWord(uint addr, ushort value)
    {
        addr &= 0xFFFFFF;

        switch (addr)
        {
            case < 0x080000 when !OVL:
                WriteChipWord(addr, value);
                break;
            case >= 0xC00000 and < 0xC80000 when SlowRam != null:
                WriteSlowWord(addr, value);
                break;
            case >= 0xBF0000 and < 0xC00000:
                WriteCIA(addr, value);
                break;
            case >= 0xDFF000 and < 0xE00000:
                Custom.WriteRegister((addr - 0xDFF000) & 0x1FE, value);
                break;
            // ROM writes are ignored
        }
    }
}
```

### CIA Address Decoding

```
CIA-A at $BFE001 (odd bytes only):
  Register select: A8-A11 (bits 8-11 of address)
  $BFE001 = PRA, $BFE101 = PRB, $BFE201 = DDRA, ..., $BFEF01 = CRB

CIA-B at $BFD000 (even bytes only):
  Register select: A8-A11
  $BFD000 = PRA, $BFD100 = PRB, $BFD200 = DDRA, ..., $BFDF00 = CRB
```

### ROM Loading

```csharp
void LoadKickstart(string path)
{
    var rom = File.ReadAllBytes(path);
    // Kickstart 1.3 = 256 KB (262144 bytes)
    // Kickstart 1.2 = 256 KB
    // Earlier = 256 KB (some are 512 KB for later versions)
    if (rom.Length == 262144 || rom.Length == 524288)
    {
        KickstartRom = rom;
        OVL = true; // ROM overlay active at reset
    }
}
```

## Chip RAM DMA Access

Chip RAM is dual-ported: accessible by both CPU and DMA engine (Agnus).

```csharp
// Agnus DMA reads (bypasses address decoder — direct chip RAM access)
ushort DmaReadWord(uint addr)
{
    addr &= 0x7FFFF; // 512 KB mask (OCS)
    return (ushort)(ChipRam[addr] << 8 | ChipRam[addr + 1]);
}

void DmaWriteWord(uint addr, ushort value)
{
    addr &= 0x7FFFF;
    ChipRam[addr] = (byte)(value >> 8);
    ChipRam[addr + 1] = (byte)(value & 0xFF);
}
```
