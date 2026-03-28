# Technical Spec: CIA Chips, Floppy Disk, and I/O

## CIA 8520 Implementation

### Core Structure

```csharp
class Cia8520
{
    // Ports
    byte PRA, PRB;         // Port data registers
    byte DDRA, DDRB;       // Data direction (1=output, 0=input)

    // Timers
    ushort TimerALatch, TimerBLatch;
    ushort TimerACounter, TimerBCounter;
    byte CRA, CRB;        // Control registers

    // TOD (Time-of-Day / Event Counter)
    uint TOD;              // 24-bit counter
    uint TODAlarm;         // 24-bit alarm
    uint TODLatch;         // Latched value for reading
    bool TODLatched;       // Reading in progress

    // Serial
    byte SDR;              // Serial data register

    // Interrupts
    byte ICRMask;          // Interrupt enable mask
    byte ICRData;          // Interrupt flags
    bool InterruptOut;     // IRQ line state

    byte ReadPort(byte portData, byte ddr, byte externalInput)
    {
        // Output bits come from port register, input bits from external
        return (byte)((portData & ddr) | (externalInput & ~ddr));
    }

    void TickTimers()
    {
        // Timer A
        if ((CRA & 0x01) != 0) // Timer A running
        {
            bool countPhi2 = (CRA & 0x20) == 0; // Count system clocks (not CNT pin)
            if (countPhi2)
            {
                if (--TimerACounter == 0)
                {
                    TimerACounter = TimerALatch;
                    SetICR(0x01); // Timer A underflow flag

                    if ((CRA & 0x08) != 0) // One-shot mode
                        CRA &= 0xFE; // Stop timer

                    // If CRB set to count Timer A underflows:
                    if ((CRB & 0x61) == 0x41) // Timer B counts A underflows
                        TickTimerB();
                }
            }
        }

        // Timer B (direct phi2 counting)
        if ((CRB & 0x01) != 0 && (CRB & 0x60) == 0x00)
        {
            if (--TimerBCounter == 0)
            {
                TimerBCounter = TimerBLatch;
                SetICR(0x02); // Timer B underflow flag
                if ((CRB & 0x08) != 0) CRB &= 0xFE;
            }
        }
    }

    void SetICR(byte flags)
    {
        ICRData |= flags;
        if ((ICRData & ICRMask) != 0)
        {
            ICRData |= 0x80; // IR flag
            InterruptOut = true;
        }
    }

    byte ReadICR()
    {
        byte result = ICRData;
        ICRData = 0;
        InterruptOut = false;
        return result;
    }

    void WriteICR(byte value)
    {
        if ((value & 0x80) != 0)
            ICRMask |= (byte)(value & 0x7F);  // SET
        else
            ICRMask &= (byte)~(value & 0x7F); // CLEAR
    }
}
```

### CIA-A Specific Behavior

```csharp
class CiaA : Cia8520
{
    // Port A reads: directly directly directly directly directly directly directly directly directly directly directly directly fire buttons, disk signals, LED, OVL
    // Port B: keyboard serial data

    // Keyboard handshake
    byte KeyboardData;
    bool KeyboardAck;

    void HandleKeyboardByte(byte scancode)
    {
        // Keyboard controller sends inverted, rotated scan code
        KeyboardData = scancode;
        SetICR(0x08); // SP flag — serial port interrupt
        // This triggers CIA-A ICR → PORTS interrupt → IPL level 2
    }

    // OVL bit (Port A bit 0): controls Kickstart ROM overlay
    bool OverlayActive => (ReadPort(PRA, DDRA, 0) & 0x01) != 0;
}
```

### CIA-B Specific Behavior

```csharp
class CiaB : Cia8520
{
    // Port A: serial port control lines
    // Port B: disk drive control

    // Disk drive signals (active low, accent accent accent accent accent accent accent accent accent accent accent accent accent)
    bool MotorOn => (PRB & 0x80) == 0;
    int SelectedDrive
    {
        get
        {
            if ((PRB & 0x08) == 0) return 0; // SEL0 (internal)
            if ((PRB & 0x10) == 0) return 1; // SEL1
            if ((PRB & 0x20) == 0) return 2; // SEL2
            if ((PRB & 0x40) == 0) return 3; // SEL3
            return -1;
        }
    }
    bool HeadSide => (PRB & 0x04) != 0;  // 0=upper, 1=lower
    bool StepDirection => (PRB & 0x02) != 0; // 0=toward center, 1=toward edge
    // STEP pulse: transition on bit 0
}
```

## Floppy Disk Implementation

### ADF File Reader

```csharp
class AdfDisk
{
    const int TracksPerSide = 80;
    const int Sides = 2;
    const int SectorsPerTrack = 11;
    const int BytesPerSector = 512;
    const int BytesPerTrack = SectorsPerTrack * BytesPerSector; // 5632
    const int TotalSize = TracksPerSide * Sides * BytesPerTrack; // 901120

    byte[] Data; // Raw ADF data (901120 bytes)

    void Load(string path)
    {
        Data = File.ReadAllBytes(path);
        if (Data.Length != TotalSize)
            throw new InvalidDataException($"ADF must be {TotalSize} bytes, got {Data.Length}");
    }

    // Track numbering in ADF: track 0 = cylinder 0 side 0, track 1 = cylinder 0 side 1, etc.
    byte[] ReadTrack(int cylinder, int side)
    {
        int trackNum = cylinder * 2 + side;
        int offset = trackNum * BytesPerTrack;
        return Data[offset..(offset + BytesPerTrack)];
    }

    byte[] ReadSector(int cylinder, int side, int sector)
    {
        var track = ReadTrack(cylinder, side);
        int offset = sector * BytesPerSector;
        return track[offset..(offset + BytesPerSector)];
    }
}
```

### Drive State Machine

```csharp
class FloppyDrive
{
    AdfDisk Disk;
    int CurrentCylinder;   // 0-79
    int CurrentSide;       // 0 or 1
    bool MotorRunning;
    bool DiskInserted;
    int RotationPosition;  // 0-11 (current sector under head)

    // Track buffer — loaded when head position changes
    byte[] TrackBuffer;
    int TrackBufferOffset;

    // Drive ready: motor spinning + disk inserted + at speed
    bool Ready => MotorRunning && DiskInserted;

    void StepHead(bool directionInward)
    {
        if (directionInward && CurrentCylinder < 79)
            CurrentCylinder++;
        else if (!directionInward && CurrentCylinder > 0)
            CurrentCylinder--;

        LoadTrackBuffer();
    }

    void SelectSide(int side)
    {
        CurrentSide = side;
        LoadTrackBuffer();
    }

    void LoadTrackBuffer()
    {
        if (Disk != null)
            TrackBuffer = Disk.ReadTrack(CurrentCylinder, CurrentSide);
    }

    // DMA reads sequential words from the track buffer
    ushort ReadNextWord()
    {
        if (TrackBuffer == null) return 0;
        ushort word = (ushort)(TrackBuffer[TrackBufferOffset] << 8 | TrackBuffer[TrackBufferOffset + 1]);
        TrackBufferOffset = (TrackBufferOffset + 2) % TrackBuffer.Length;
        return word;
    }
}
```

### Disk DMA Controller

```csharp
class DiskDma
{
    // Registers
    uint DskPt;          // DMA pointer (chip RAM destination)
    ushort DskLen;       // Length and direction
    ushort DskSync;      // Sync word (typically $4489)

    bool DmaEnabled;
    bool SyncFound;
    int WordsRemaining;
    bool Writing;

    void WriteDSKLEN(ushort value)
    {
        // DMA starts on second consecutive write with bit 15 set
        if ((value & 0x8000) != 0 && (DskLen & 0x8000) != 0)
        {
            // Start DMA
            WordsRemaining = value & 0x3FFF;
            Writing = (value & 0x4000) != 0;
            DmaEnabled = true;
            SyncFound = false;
        }
        DskLen = value;
    }

    // Called during disk DMA slots
    void ExecuteDmaCycle(FloppyDrive drive)
    {
        if (!DmaEnabled || WordsRemaining == 0) return;

        ushort word = drive.ReadNextWord();

        // Wait for sync word before reading data
        if (!SyncFound)
        {
            if (word == DskSync)
                SyncFound = true;
            return;
        }

        // Write decoded data to chip RAM
        DmaWriteWord(DskPt, word);
        DskPt += 2;
        WordsRemaining--;

        if (WordsRemaining == 0)
        {
            DmaEnabled = false;
            // Trigger DSKBLK interrupt
        }
    }
}
```

## Keyboard Protocol

### Key Transmission

```csharp
class KeyboardInterface
{
    Queue<byte> KeyBuffer = new();

    void KeyPress(int hostKeyCode)
    {
        byte amigaCode = MapToAmigaScanCode(hostKeyCode);
        // Key pressed: bit 7 clear
        KeyBuffer.Enqueue((byte)(amigaCode << 1 | 0)); // Amiga sends code << 1, inverted
    }

    void KeyRelease(int hostKeyCode)
    {
        byte amigaCode = MapToAmigaScanCode(hostKeyCode);
        // Key released: bit 7 set
        KeyBuffer.Enqueue((byte)((amigaCode << 1 | 1)));
    }

    // CIA-A reads the serial data, rotates left 1, inverts
    // to recover the original scan code
    byte DecodeKeyData(byte raw)
    {
        return (byte)~((raw >> 1) | (raw << 7));
    }
}
```

## Joystick/Mouse

### Mouse Quadrature

```csharp
class MousePort
{
    byte XCounter, YCounter;

    void Move(int dx, int dy)
    {
        XCounter = (byte)(XCounter + dx);
        YCounter = (byte)(YCounter + dy);
    }

    ushort ReadJOYDAT()
    {
        // Bits 15-8: Y counter, Bits 7-0: X counter
        return (ushort)(YCounter << 8 | XCounter);
    }
}
```

### Joystick Digital

```csharp
ushort ReadJoystick(bool up, bool down, bool left, bool right)
{
    // Encode as quadrature-style bits for JOYxDAT
    int v1 = down ? 1 : 0;
    int v0 = (down ^ up) ? 1 : 0;
    int h1 = right ? 1 : 0;
    int h0 = (right ^ left) ? 1 : 0;
    return (ushort)((v1 << 9) | (v0 << 8) | (h1 << 1) | h0);
}
```
