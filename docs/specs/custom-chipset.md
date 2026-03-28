# Technical Spec: Custom Chipset Implementation (Agnus, Denise, Paula)

## Agnus — DMA Controller and Bus Arbiter

### DMA Slot Allocation Per Scanline

Each PAL scanline = 227.5 color clocks (CCK). DMA slots are allocated in a fixed pattern:

```
Cycle positions (hex, color clocks):
$00-$03: Refresh DMA (4 slots)
$04-$06: Disk DMA (3 slots, when enabled)
$07-$0A: Audio DMA (4 slots: AUD0-AUD3)
$0B-$2A: Sprite DMA (16 slots: 2 per sprite × 8 sprites)
$2B-$E2: Display DMA (bitplane fetches, varies by mode)
         Lores: starts at $38, 2 fetches per bitplane per 8 pixels
         Hires: starts at $38, 4 fetches per bitplane per 8 pixels
Remaining: Available to Copper, Blitter, CPU
```

### Implementation Structure

```csharp
class Agnus
{
    // Beam position
    int HPos;          // Horizontal position (0-227)
    int VPos;          // Vertical position (0-312 PAL)
    bool LongFrame;    // Interlace: alternates each frame

    // DMA control
    ushort DMACON;     // DMA enable flags
    bool DMAEnabled(DmaChannel ch) => (DMACON & (1 << (int)ch)) != 0 && (DMACON & 0x200) != 0;

    // DMA pointers (auto-incrementing)
    uint[] BplPt = new uint[6];   // Bitplane pointers
    uint[] SprPt = new uint[8];   // Sprite pointers
    uint[] AudPt = new uint[4];   // Audio channel pointers
    uint DskPt;                    // Disk DMA pointer
    uint CopPc;                    // Copper program counter

    // Per-cycle execution
    void ExecuteCycle()
    {
        // 1. Determine DMA slot owner for current HPos
        // 2. If DMA channel active: perform transfer
        // 3. Advance beam position
        // 4. Check for VBLANK, HBLANK events
        AdvanceBeam();
    }

    void AdvanceBeam()
    {
        HPos++;
        if (HPos >= 227) // PAL line length
        {
            HPos = 0;
            VPos++;
            if (VPos >= 312) // PAL frame height
            {
                VPos = 0;
                LongFrame = !LongFrame; // Interlace toggle
                // Trigger VBLANK interrupt
            }
        }
    }
}
```

### DMACON Register ($DFF096 write, $DFF002 read)

```
Bit 15: SET/CLR (1=set bits, 0=clear bits)
Bit 9:  BBUSY (blitter busy, read-only in DMACONR)
Bit 8:  BZERO (blitter zero flag, read-only)
Bit 9:  DMAEN (master DMA enable)
Bit 8:  BPLEN (bitplane DMA)
Bit 7:  COPEN (Copper DMA)
Bit 6:  BLTEN (Blitter DMA)
Bit 5:  SPREN (Sprite DMA)
Bit 4:  DSKEN (Disk DMA)
Bit 3:  AUD3EN (Audio channel 3)
Bit 2:  AUD2EN
Bit 1:  AUD1EN
Bit 0:  AUD0EN
```

## Denise — Video Output

### Pixel Pipeline

Each color clock, Denise:
1. Shifts bitplane data registers (BPLxDAT) → extracts pixel bits
2. Composites bitplanes → palette index (0-31 or 0-63 for EHB)
3. Checks sprite data → overlay if opaque pixel
4. Applies priority (playfield vs sprites via BPLCON2)
5. Collision detection update
6. Outputs 12-bit RGB from COLORxx register

### Display Mode Implementation

```csharp
class Denise
{
    ushort[] Color = new ushort[32];     // COLOR00-COLOR31
    ushort[] BplDat = new ushort[6];     // Bitplane shift registers
    ushort BPLCON0, BPLCON1, BPLCON2;

    int NumBitplanes => (BPLCON0 >> 12) & 7;
    bool IsHires => (BPLCON0 & 0x8000) != 0;
    bool IsHAM => (BPLCON0 & 0x0800) != 0;
    bool IsDualPlayfield => (BPLCON0 & 0x0400) != 0;
    bool IsEHB => NumBitplanes == 6 && !IsHAM;

    ushort GetPixelColor(int x)
    {
        int index = 0;
        for (int i = 0; i < NumBitplanes; i++)
        {
            int bit = (BplDat[i] >> (15 - (x & 15))) & 1;
            index |= bit << i;
        }

        if (IsHAM)
            return DecodeHAM(index);
        if (IsEHB && index >= 32)
            return (ushort)(Color[index - 32] >> 1 & 0x777); // Half brightness
        return Color[index];
    }

    ushort hamPrevColor;

    ushort DecodeHAM(int data)
    {
        int control = (data >> 4) & 3;
        int value = data & 0xF;

        return control switch
        {
            0 => hamPrevColor = Color[value],                                    // Set from palette
            1 => hamPrevColor = (ushort)((hamPrevColor & 0xFF0) | value),        // Modify blue
            2 => hamPrevColor = (ushort)((hamPrevColor & 0x0FF) | (value << 8)), // Modify red
            3 => hamPrevColor = (ushort)((hamPrevColor & 0xF0F) | (value << 4)), // Modify green
        };
    }
}
```

### Sprite Implementation

```csharp
class SpriteUnit
{
    // Per sprite: position, data, arm/disarm state
    struct SpriteState
    {
        public int VStart, VStop, HStart;
        public ushort DataA, DataB;    // Two bitplane words
        public bool Armed;
    }

    SpriteState[] Sprites = new SpriteState[8];

    // Called each horizontal position
    ushort? GetSpritePixel(int hpos, int vpos)
    {
        for (int i = 7; i >= 0; i--) // Lower index = higher priority
        {
            var s = Sprites[i];
            if (!s.Armed || vpos < s.VStart || vpos >= s.VStop)
                continue;
            if (hpos < s.HStart || hpos >= s.HStart + 16)
                continue;

            int bit = 15 - (hpos - s.HStart);
            int pixel = ((s.DataA >> bit) & 1) | (((s.DataB >> bit) & 1) << 1);
            if (pixel == 0) continue; // Transparent

            // Sprite colors: COLOR17-19 (sprite 0-1), COLOR21-23 (2-3), etc.
            int colorBase = 16 + (i / 2) * 4;
            return Color[colorBase + pixel];
        }
        return null; // No sprite pixel
    }
}
```

### Collision Detection

```csharp
// CLXDAT ($DFF00E) — read and auto-clear
// Bits 14-0: collision pairs
// Bit 14: sprite 4/6 - sprite 5/7
// ...
// Bit 0: playfield 1 - playfield 2

// Each pixel: check all active objects, set corresponding bits
void UpdateCollision(bool[] spriteActive, bool pf1Active, bool pf2Active)
{
    // Sprite-playfield collisions
    for (int i = 0; i < 8; i += 2)
    {
        if (spriteActive[i] || spriteActive[i + 1])
        {
            if (pf1Active) CLXDAT |= (ushort)(1 << (1 + i / 2));  // Odd playfield
            if (pf2Active) CLXDAT |= (ushort)(1 << (5 + i / 2));  // Even playfield
        }
    }
    // Sprite-sprite collisions (pairs)
    // ... similar bit-setting logic
}
```

## Paula — Audio and Interrupts

### Audio Channel State Machine

Each audio channel runs independently through these states:

```
IDLE → PENDING (DMA fetches first word)
     → ACTIVE (playing samples)
     → IDLE (length counter exhausted, interrupt)
```

```csharp
class AudioChannel
{
    enum State { Idle, Pending, Active }

    uint LocationPtr;     // AUDxLC — sample pointer (set by CPU/Copper)
    ushort Length;         // AUDxLEN — remaining words
    ushort Period;         // AUDxPER — playback rate
    ushort Volume;         // AUDxVOL — 0-64

    ushort CurrentData;    // AUDxDAT — current sample pair
    int PeriodCounter;     // Counts down each cycle
    bool UseHighByte;      // Alternates between high/low byte of data word
    State ChannelState;

    // Internal working copies (reloaded from pointers when length exhausts)
    uint WorkingPtr;
    ushort WorkingLength;

    sbyte GetSample()
    {
        // Called when PeriodCounter reaches 0
        PeriodCounter = Period;
        byte sample = UseHighByte ? (byte)(CurrentData >> 8) : (byte)(CurrentData & 0xFF);
        UseHighByte = !UseHighByte;

        if (!UseHighByte) // Just used low byte — need next word
        {
            // DMA fetches next word from WorkingPtr
            // WorkingPtr += 2; WorkingLength--;
            // If WorkingLength == 0: reload from LocationPtr/Length, trigger interrupt
        }

        return (sbyte)sample;
    }

    // Mix: sample * volume / 64, then sum left (ch0+ch3) and right (ch1+ch2)
}
```

### Interrupt Controller

```csharp
class InterruptController
{
    ushort INTENA;    // Interrupt enable ($DFF09A write)
    ushort INTREQ;    // Interrupt request ($DFF09C write)

    // Both use SET/CLR protocol: bit 15 = 1 means SET flagged bits, 0 = CLEAR

    void WriteINTENA(ushort value)
    {
        if ((value & 0x8000) != 0)
            INTENA |= (ushort)(value & 0x7FFF);
        else
            INTENA &= (ushort)~(value & 0x7FFF);
    }

    void WriteINTREQ(ushort value)
    {
        if ((value & 0x8000) != 0)
            INTREQ |= (ushort)(value & 0x7FFF);
        else
            INTREQ &= (ushort)~(value & 0x7FFF);
    }

    // Map to 68000 interrupt levels
    int GetPendingLevel()
    {
        ushort active = (ushort)(INTENA & INTREQ & 0x3FFF);
        if ((INTENA & 0x4000) == 0) return 0; // Master enable off

        if ((active & 0x2000) != 0) return 6; // EXTER
        if ((active & 0x1800) != 0) return 5; // RBF, DSKSYN
        if ((active & 0x0780) != 0) return 4; // AUD0-3
        if ((active & 0x0070) != 0) return 3; // COPER, VERTB, BLIT
        if ((active & 0x0008) != 0) return 2; // PORTS
        if ((active & 0x0007) != 0) return 1; // TBE, DSKBLK, SOFT
        return 0;
    }
}
```

### Interrupt Bit Map

```
Bit 14: INTEN (master enable)
Bit 13: EXTER (CIA-B → level 6)
Bit 12: DSKSYN (disk sync → level 5)
Bit 11: RBF (serial receive → level 5)
Bit 10: AUD3 (audio channel 3 → level 4)
Bit 9:  AUD2 (audio channel 2 → level 4)
Bit 8:  AUD1 (audio channel 1 → level 4)
Bit 7:  AUD0 (audio channel 0 → level 4)
Bit 6:  BLIT (blitter done → level 3)
Bit 5:  VERTB (vertical blank → level 3)
Bit 4:  COPER (Copper → level 3)
Bit 3:  PORTS (CIA-A → level 2)
Bit 2:  SOFT (software interrupt → level 1)
Bit 1:  DSKBLK (disk block done → level 1)
Bit 0:  TBE (serial transmit empty → level 1)
```

## Custom Chip Register Map ($DFF000–$DFF1FE)

### Register Access Rules

- All registers are 16-bit, accessed at even addresses
- Most are write-only (read returns undefined/old data)
- Read-only registers: VPOSR, VHPOSR, DMACONR, INTENAR, INTREQR, JOY0DAT, JOY1DAT, CLXDAT, POTGOR, SERDATR, DSKBYTR
- SET/CLR protocol: DMACON, INTENA, INTREQ, ADKCON — bit 15 determines set or clear

### Register Map Implementation

```csharp
class CustomChipRegisters
{
    // Indexed by (address - 0xDFF000) / 2
    ushort ReadRegister(uint offset)
    {
        return offset switch
        {
            0x000 => BLTDDAT,    // (unused read)
            0x002 => DMACONR,
            0x004 => VPOSR,
            0x006 => VHPOSR,
            0x008 => DSKDATR,
            0x00A => JOY0DAT,
            0x00C => JOY1DAT,
            0x00E => CLXDAT,     // read-and-clear
            0x010 => ADKCONR,
            0x012 => POT0DAT,
            0x014 => POT1DAT,
            0x016 => POTGOR,
            0x018 => SERDATR,
            0x01A => DSKBYTR,
            0x01C => INTENAR,
            0x01E => INTREQR,
            _ => 0xFFFF          // Write-only registers return undefined
        };
    }

    void WriteRegister(uint offset, ushort value)
    {
        switch (offset)
        {
            // Agnus registers
            case 0x020: DskPtH(value); break;    // DSKPTH
            case 0x022: DskPtL(value); break;    // DSKPTL
            case 0x024: DSKLEN = value; break;
            case 0x026: DSKDAT = value; break;
            // ... hundreds more entries
            case 0x096: WriteDMACON(value); break;
            case 0x09A: WriteINTENA(value); break;
            case 0x09C: WriteINTREQ(value); break;
            // Color registers
            case >= 0x180 and <= 0x1BE:
                Color[(offset - 0x180) / 2] = value; break;
        }
    }
}
```

## Timing Synchronization

### Cycle-Level Execution Model

The emulator runs in color clock (CCK) steps. Each CCK:
1. Agnus determines DMA slot owner
2. If CPU has bus: CPU executes (2 CPU cycles per CCK)
3. Copper processes if its turn
4. Blitter processes if its turn
5. Denise shifts bitplane data, outputs pixel
6. Paula decrements audio period counters

### Synchronization Points

- VBLANK (line 0): Restart Copper, trigger VERTB interrupt
- HBLANK (end of visible line): Bitplane DMA refill, sprite DMA
- Line-level: Audio DMA happens once per line per channel
- Color clock level: Bitplane shift, Copper WAIT resolution
