# Technical Spec: Blitter and Copper

## Blitter Implementation

### Blit Operation Pipeline

```csharp
class Blitter
{
    // Pointers
    uint APtr, BPtr, CPtr, DPtr;
    // Modulos (added at end of each row)
    short AMod, BMod, CMod, DMod;
    // Shift values
    int AShift, BShift;
    // Masks
    ushort FirstWordMask, LastWordMask;
    // Control
    ushort BLTCON0, BLTCON1;
    // Size
    int Width;   // in words
    int Height;  // in lines

    byte Minterm => (byte)(BLTCON0 & 0xFF);
    bool UseA => (BLTCON0 & 0x0800) != 0;
    bool UseB => (BLTCON0 & 0x0400) != 0;
    bool UseC => (BLTCON0 & 0x0200) != 0;
    bool UseD => (BLTCON0 & 0x0100) != 0;
    bool IsLineMode => (BLTCON1 & 0x0001) != 0;
    bool FillMode => (BLTCON1 & 0x0018) != 0;
    bool DescendingMode => (BLTCON1 & 0x0002) != 0;

    bool Busy;
    bool ZeroFlag;

    void StartBlit(ushort sizeWord)
    {
        Height = (sizeWord >> 6) & 0x3FF;
        Width = sizeWord & 0x3F;
        if (Height == 0) Height = 1024;
        if (Width == 0) Width = 64;
        Busy = true;
        ZeroFlag = true;
    }

    // Called each DMA cycle allocated to Blitter
    void ExecuteCycle()
    {
        if (!Busy) return;

        // Process one word per DMA cycle (simplified — actual timing is per-channel)
        for (int y = 0; y < Height; y++)
        {
            ushort prevCarry = 0; // For fill mode
            for (int x = 0; x < Width; x++)
            {
                ushort a = UseA ? DmaRead(APtr) : 0;
                ushort b = UseB ? DmaRead(BPtr) : 0;
                ushort c = UseC ? DmaRead(CPtr) : 0;

                // Apply shift
                // ... barrel shift A and B by AShift/BShift bits

                // Apply first/last word mask to A
                if (x == 0) a &= FirstWordMask;
                if (x == Width - 1) a &= LastWordMask;

                // Apply minterm
                ushort result = ApplyMinterm(a, b, c, Minterm);

                // Apply fill if enabled
                if (FillMode)
                    result = ApplyFill(result, ref prevCarry);

                if (result != 0) ZeroFlag = false;

                if (UseD) DmaWrite(DPtr, result);

                // Advance pointers
                int step = DescendingMode ? -2 : 2;
                if (UseA) APtr = (uint)(APtr + step);
                if (UseB) BPtr = (uint)(BPtr + step);
                if (UseC) CPtr = (uint)(CPtr + step);
                if (UseD) DPtr = (uint)(DPtr + step);
            }
            // Apply modulos at end of row
            if (UseA) APtr = (uint)(APtr + AMod);
            if (UseB) BPtr = (uint)(BPtr + BMod);
            if (UseC) CPtr = (uint)(CPtr + CMod);
            if (UseD) DPtr = (uint)(DPtr + DMod);
        }

        Busy = false;
        // Trigger BLIT interrupt
    }

    static ushort ApplyMinterm(ushort a, ushort b, ushort c, byte minterm)
    {
        ushort result = 0;
        // Minterm is 8-bit truth table for (a,b,c) combinations
        if ((minterm & 0x80) != 0) result |= (ushort)(a & b & c);
        if ((minterm & 0x40) != 0) result |= (ushort)(a & b & ~c);
        if ((minterm & 0x20) != 0) result |= (ushort)(a & ~b & c);
        if ((minterm & 0x10) != 0) result |= (ushort)(a & ~b & ~c);
        if ((minterm & 0x08) != 0) result |= (ushort)(~a & b & c);
        if ((minterm & 0x04) != 0) result |= (ushort)(~a & b & ~c);
        if ((minterm & 0x02) != 0) result |= (ushort)(~a & ~b & c);
        if ((minterm & 0x01) != 0) result |= (ushort)(~a & ~b & ~c);
        return result;
    }
}
```

### Line Draw Mode

```csharp
void DrawLine()
{
    // Bresenham line drawing in hardware
    // A channel: single pixel pattern (texture)
    // C channel: existing destination data (for OR/XOR merge)
    // D channel: output
    // BLTCON1 bits 4-6: octant selection
    // APtr holds Bresenham accumulator
    // BMod = 4 * min(dx,dy) - 2 * max(dx,dy)  (error increment when moving diagonal)
    // AMod = 4 * min(dx,dy)                     (error increment when moving straight)
}
```

## Copper Implementation

### Copper Instruction Execution

```csharp
class Copper
{
    uint COP1LC, COP2LC;   // Copper list pointers
    uint PC;                // Current instruction pointer
    bool Enabled;
    CopperState State;

    enum CopperState { FetchFirst, FetchSecond, Execute, Waiting }

    ushort IR1, IR2;        // Instruction register words

    void ExecuteCycle(int hpos, int vpos)
    {
        if (!Enabled) return;

        switch (State)
        {
            case CopperState.FetchFirst:
                IR1 = DmaRead(PC); PC += 2;
                State = CopperState.FetchSecond;
                break;

            case CopperState.FetchSecond:
                IR2 = DmaRead(PC); PC += 2;
                State = CopperState.Execute;
                break;

            case CopperState.Execute:
                if ((IR1 & 1) == 0)
                {
                    // MOVE instruction
                    uint reg = (uint)(IR1 & 0x1FE);
                    // Safety: prevent writes to dangerous registers unless COPCON allows
                    if (reg >= 0x040 || CopperDangerBit)
                        Custom.WriteRegister(reg, IR2);
                }
                else
                {
                    // WAIT or SKIP
                    bool isSkip = (IR2 & 1) != 0;
                    int waitVP = (IR1 >> 8) & 0xFF;
                    int waitHP = IR1 & 0xFE;
                    int maskVP = (IR2 >> 8) & 0x7F;
                    int maskHP = IR2 & 0xFE;
                    bool bfd = (IR2 & 0x8000) == 0; // Blitter-finish-disable

                    if (isSkip)
                    {
                        if (BeamMatch(hpos, vpos, waitHP, waitVP, maskHP, maskVP))
                            PC += 4; // Skip next instruction
                    }
                    else
                    {
                        // WAIT: check if beam has passed the target
                        if (!BeamMatch(hpos, vpos, waitHP, waitVP, maskHP, maskVP))
                        {
                            State = CopperState.Waiting;
                            return;
                        }
                        // Special: WAIT $FFFF,$FFFE = wait forever (end of copper list)
                    }
                }
                State = CopperState.FetchFirst;
                break;

            case CopperState.Waiting:
                // Re-check beam position each cycle
                int waitVP2 = (IR1 >> 8) & 0xFF;
                int waitHP2 = IR1 & 0xFE;
                int maskVP2 = (IR2 >> 8) & 0x7F;
                int maskHP2 = IR2 & 0xFE;
                if (BeamMatch(hpos, vpos, waitHP2, waitVP2, maskHP2, maskVP2))
                    State = CopperState.FetchFirst;
                break;
        }
    }

    bool BeamMatch(int hpos, int vpos, int hp, int vp, int hm, int vm)
    {
        return (vpos & vm) >= (vp & vm) &&
               ((vpos & vm) > (vp & vm) || (hpos & hm) >= (hp & hm));
    }

    // VBLANK: restart Copper
    void OnVerticalBlank()
    {
        PC = COP1LC;
        State = CopperState.FetchFirst;
    }
}
```

## Blitter Timing

### DMA Cycles Per Blit Word

| Channels Used | Cycles per word |
|---------------|----------------|
| D only | 1 |
| A+D | 2 |
| B+D, C+D | 2 |
| A+B+D, A+C+D, B+C+D | 3 |
| A+B+C+D | 4 |

### Bus Sharing

- Blitter has lowest DMA priority but can "nasty" the CPU (BLTPRI in DMACON)
- When BLTPRI=1: Blitter takes every free bus cycle, CPU gets nothing
- When BLTPRI=0: Blitter and CPU alternate
- CPU can still access fast/slow RAM while Blitter uses chip RAM bus
