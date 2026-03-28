# Commodore Amiga 500 — System Architecture

## Overview

The Amiga 500 (A500) is a 16/32-bit home computer released in 1987. Its architecture centers on a Motorola 68000 CPU coordinated with three custom chipset ICs (Agnus, Denise, Paula) that handle graphics, audio, and DMA independently. Two 8520 CIA chips manage I/O and timing. The system boots from Kickstart ROM (typically v1.3) which provides the OS kernel and libraries.

## 1. Motorola 68000 CPU

### Registers
- 8 data registers: D0–D7 (32-bit)
- 8 address registers: A0–A7 (32-bit), A7 is the stack pointer
- Program Counter (PC): 32-bit
- Status Register (SR): 16-bit (CCR in low byte, system byte in high)
- Two stack pointers: USP (user) and SSP (supervisor), switched via SR S-bit

### Key Characteristics
- 16-bit external data bus, 32-bit internal
- 24-bit address bus → 16 MB addressable space (A1–A23, A0 implicit via UDS/LDS)
- Big-endian byte ordering
- 56 base instruction types with 14 addressing modes
- Exception processing: vector table at $000000–$0003FF
- Instruction prefetch: 2-word prefetch queue
- Minimum instruction time: 4 clock cycles
- Clock: 7.09379 MHz (PAL) / 7.15909 MHz (NTSC)

### Instruction Categories
- Data movement: MOVE, MOVEA, MOVEM, MOVEQ, LEA, PEA, EXG, SWAP
- Arithmetic: ADD, SUB, MUL (signed/unsigned), DIV (signed/unsigned), NEG, EXT, CLR
- Logic: AND, OR, EOR, NOT
- Shift/Rotate: ASL, ASR, LSL, LSR, ROL, ROR, ROXL, ROXR
- Bit manipulation: BTST, BSET, BCLR, BCHG
- BCD: ABCD, SBCD, NBCD
- Control flow: Bcc (14 conditions), DBcc, Scc, JMP, JSR, RTS, RTE, RTR
- System: TRAP, RESET, STOP, NOP, LINK, UNLK, MOVE to/from SR/USP

### Exception Handling
- Priority levels 0–7 (7 = NMI, non-maskable)
- Vector table: 256 vectors × 4 bytes = 1024 bytes at $000000
- Key vectors: Reset ($000000), Bus Error ($000008), Address Error ($00000C), Illegal Instruction ($000010), TRAP #0–15 ($000080–$0000BC), Auto-vectors ($000064–$00007C)
- Interrupt levels: IPL0–IPL2 pins encode level; levels 1–6 maskable by SR I-bits

## 2. Agnus (8361/8370/8372)

Agnus is the bus controller and DMA coordinator. It owns the system bus during DMA cycles.

### DMA Channels (25 total)
- **Disk DMA:** 3 channels (MFM data read/write)
- **Audio DMA:** 4 channels (one per Paula audio channel)
- **Sprite DMA:** 8 channels (one per hardware sprite)
- **Bitplane DMA:** 6 channels (up to 6 bitplanes in lores)
- **Blitter DMA:** 4 source/dest channels (A, B, C, D)
- **Copper DMA:** 1 channel (Copper instruction fetch)

### DMA Priority (highest to lowest)
1. Disk
2. Audio
3. Sprites
4. Bitplanes
5. Copper
6. Blitter (lowest DMA priority, but steals bus from CPU)
7. CPU (gets bus only when no DMA active)

### Beam Counters
- VHPOSR/VHPOSW: vertical ($000–$312 PAL) and horizontal ($000–$1C7) beam position
- VPOSR/VPOSW: long frame bit, Agnus ID
- Used by Copper for synchronization

### Chip RAM Access
- OCS Agnus (8370): 512 KB chip RAM ($000000–$07FFFF)
- ECS Agnus (8372A): 1 MB chip RAM ($000000–$0FFFFF)
- Fat Agnus (8375): 2 MB chip RAM ($000000–$1FFFFF)
- Default A500 ships with 512 KB

### Register Map
- Base address: $DFF000
- DMACON ($096): DMA control — enable/disable individual DMA channels
- DMACONR ($002): Read current DMA state
- All Agnus registers are write-only except VPOSR, VHPOSR, DMACONR

## 3. Denise (8362/8373)

Denise is the video/graphics output chip.

### Display Modes
- **Lores:** 320×200 (NTSC) / 320×256 (PAL), up to 32 colors (5 bitplanes)
- **Hires:** 640×200 (NTSC) / 640×256 (PAL), up to 16 colors (4 bitplanes)
- **Interlaced:** Doubles vertical resolution (320×400 / 640×400)
- **HAM (Hold-And-Modify):** 4096 colors using 6 bitplanes (2 control + 4 data)
- **Extra Half-Brite (EHB):** 64 colors (6th bitplane halves RGB intensity)
- **Dual playfield:** Two independent 3-bitplane scroll layers

### Color Registers
- 32 color registers: COLOR00–COLOR31 ($180–$1BE)
- 12-bit RGB (4 bits per component, 4096 possible colors)
- COLOR00 is background/transparency color

### Bitplane Registers
- BPL1DAT–BPL6DAT: bitplane data shift registers
- BPLCON0 ($100): number of bitplanes, mode (hires/lores/HAM/EHB/dual-playfield)
- BPLCON1 ($102): horizontal scroll delays (4-bit per playfield)
- BPLCON2 ($104): playfield priority, sprite-playfield priority
- BPL1MOD/BPL2MOD: modulo values for odd/even bitplanes

### Sprites
- 8 hardware sprites (SPR0–SPR7)
- Each sprite: 16 pixels wide, arbitrary height
- 3 colors + transparent per sprite (2 bitplanes)
- Sprite pairs (0-1, 2-3, 4-5, 6-7) can be combined for 15-color sprites
- SPRxPT: pointer to sprite data in chip RAM
- SPRxPOS/SPRxCTL: vertical/horizontal position, start/stop lines
- SPRxDATA/SPRxDATB: 16-bit bitplane data words

### Collision Detection
- CLXDAT ($00E): sprite-sprite and sprite-playfield collision register
- CLXCON ($098): collision control (which bitplanes participate)
- Hardware-level collision checking per scanline

## 4. Paula (8364)

Paula handles audio output and interrupt control.

### Audio System
- 4 independent audio channels (AUD0–AUD3)
- 8-bit PCM samples, output through DMA
- Per-channel registers:
  - AUDxLCH/AUDxLCL: pointer to sample data in chip RAM
  - AUDxLEN: sample length in words (1 word = 2 samples)
  - AUDxPER: sample period (minimum 124 = ~28.6 kHz)
  - AUDxVOL: volume (0–64)
  - AUDxDAT: current sample data
- Channels 0+3 → left, 1+2 → right (fixed stereo separation)
- Audio modulation: period modulation (ch 1→0, 3→2) and volume modulation
- Sample frequency = clock / (2 × period), clock = 3.546895 MHz (PAL)

### Interrupt Control
- INTENA ($09A): interrupt enable register (16 bits)
- INTENAR ($01C): read interrupt enable state
- INTREQ ($09C): interrupt request register
- INTREQR ($01E): read pending interrupts

### Interrupt Sources (mapped to 68000 levels)
| Level | Sources |
|-------|---------|
| 1 | TBE (serial transmit), DSKBLK (disk block done), SOFT (software) |
| 2 | PORTS (CIA-A: keyboard, gameports, TOD) |
| 3 | COPER (Copper), VERTB (vertical blank), BLIT (Blitter done) |
| 4 | AUD0–AUD3 (audio channel interrupts) |
| 5 | RBF (serial receive), DSKSYN (disk sync found) |
| 6 | EXTER (CIA-B: disk drive, serial, TOD) |

### Floppy Disk Controller
- MFM encoding/decoding
- DMA-driven read/write
- DSKPT: pointer to disk DMA buffer
- DSKLEN: transfer length and direction
- DSKSYNC: sync word for read operations (typically $4489)
- ADKCON: disk and audio control register

## 5. Blitter

The Blitter is a DMA-driven 2D graphics accelerator inside Agnus.

### Capabilities
- Copy rectangular regions (area copy/blit)
- Draw filled polygons (area fill)
- Draw lines (Bresenham line drawing)
- Logic operations on up to 3 source and 1 destination (256 minterm combinations)
- Barrel shifting for sub-word alignment
- Masking (first/last word masks for partial-word operations)

### Channels
- **A** (source): data with optional shift and masks
- **B** (source): data with optional shift
- **C** (source): usually existing destination data (for transparency)
- **D** (destination): output

### Registers
- BLTxPT (x=A,B,C,D): DMA pointers
- BLTxMOD: modulo values (bytes to skip at end of each line)
- BLTCON0 ($040): source enable bits (ABCD), shift value for A, minterm
- BLTCON1 ($042): line/area mode, fill mode, direction bits
- BLTAFWM ($044): first word mask for channel A
- BLTALWM ($046): last word mask for channel A
- BLTSIZE ($058): starts blit — height (10 bits) × width in words (6 bits)
- BLTADAT/BLTBDAT/BLTCDAT: data registers

### Minterm Logic
- 8-bit minterm selects from 256 possible boolean functions of A, B, C
- Common minterms:
  - $F0: copy A → D (straight copy)
  - $CA: cookie-cut (A=mask, B=source, C=dest: `(A AND B) OR (NOT A AND C)`)
  - $0A: invert (D = NOT A)

### Fill Mode
- Inclusive fill: toggles fill state on each set bit
- Exclusive fill: toggles on edge transitions
- Operates on single-word columns, right-to-left within words

### Line Draw Mode
- Bresenham algorithm in hardware
- Octant selection via BLTCON1 bits
- Can draw single-pixel or textured lines

## 6. Copper

The Copper is a simple co-processor inside Agnus that executes a display-synchronized instruction list.

### Instructions (3 types, each 2 words = 4 bytes)
1. **MOVE:** Write a value to a chipset register
   - Word 1: register address (bit 0 = 0)
   - Word 2: data value
   - Can write to most custom chip registers ($080–$1FE by default)
   - COPCON register controls access to lower registers (dangerous bit)

2. **WAIT:** Wait until beam reaches a specific position
   - Word 1: VP (vertical), HP (horizontal), bit 0 = 1
   - Word 2: mask bits + BFD (blitter-finished-disable) flag
   - Horizontal compare granularity: 2 lores pixels (4 hires)
   - Vertical compare: any line value

3. **SKIP:** Skip next instruction if beam past a position
   - Same encoding as WAIT but bit 0 of word 2 = 1

### Copper Lists
- COP1LC/COP2LC: pointers to copper list 1 and 2
- COPJMP1/COPJMP2: strobe registers to restart from list 1 or 2
- Typically: COP1LC set in VERTB interrupt, Copper runs every frame
- Copper restarts at vertical blank if DMACON copper bit set

### Common Uses
- Palette changes per-scanline (gradient/rainbow effects)
- Per-line display mode switching (mixed resolutions)
- Sprite multiplexing (reposition sprites mid-frame)
- Bitplane pointer updates for scrolling
- Display window manipulation (overscan)

## 7. CIA Chips (MOS 8520 × 2)

Two Complex Interface Adapter chips handle I/O, timing, and serial communication.

### CIA-A ($BFE001, odd bytes)
- **Port A (PRA):** Directly mapped
  - Bit 7: /FIR1 (joystick/mouse port 2 fire button)
  - Bit 6: /FIR0 (joystick/mouse port 1 fire button)
  - Bit 5: /RDY (disk ready)
  - Bit 4: /TK0 (disk track 0 sensor)
  - Bit 3: /WPRO (disk write protect)
  - Bit 2: /CHNG (disk change)
  - Bit 1: LED (power LED, accent color select)
  - Bit 0: OVL (Kickstart overlay)
- **Port B (PRB):** Directly mapped
  - Bits 7–0: Keyboard serial data (directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly)
- **Timer A:** 16-bit countdown, modes: one-shot/continuous, count system clocks or CNT pin
- **Timer B:** 16-bit countdown, same modes plus count Timer A underflows
- **TOD (Time of Day):** 24-bit counter clocked by 50/60 Hz line frequency
  - Registers: TOD_HI (bits 23–16), TOD_MID (bits 15–8), TOD_LO (bits 7–0)
  - Also serves as alarm
- **Interrupts → Level 2** (PORTS)

### CIA-B ($BFD000, even bytes)
- **Port A (PRA):** Directly mapped
  - Bit 7: /DTR (serial port data terminal ready)
  - Bit 6: /RTS (serial port request to send)
  - Bit 5: /CD (serial carrier detect)
  - Bit 4: /CTS (serial clear to send)
  - Bit 3: /DSR (serial data set ready)
  - Bit 2: SEL (parallel port select, accent color bit)
  - Bit 1: POUT (parallel port paper out)
  - Bit 0: BUSY (parallel port busy)
- **Port B (PRB):** Disk drive control
  - Bit 7: /MTR (motor on)
  - Bit 6: /SEL3 (select drive 3)
  - Bit 5: /SEL2 (select drive 2)
  - Bit 4: /SEL1 (select drive 1)
  - Bit 3: /SEL0 (select drive 0, internal)
  - Bit 2: /SIDE (disk head select, 0=upper)
  - Bit 1: DIR (step direction, 0=toward center)
  - Bit 0: /STEP (step head pulse)
- **Timer A/B:** Same as CIA-A
- **TOD:** Same as CIA-A
- **Interrupts → Level 6** (EXTER)

### Register Map (both CIAs)
| Offset | Name | Description |
|--------|------|-------------|
| $000 | PRA | Port A data |
| $100 | PRB | Port B data |
| $200 | DDRA | Data direction A |
| $300 | DDRB | Data direction B |
| $400 | TALO | Timer A low byte |
| $500 | TAHI | Timer A high byte |
| $600 | TBLO | Timer B low byte |
| $700 | TBHI | Timer B high byte |
| $800 | TOD_LO | Event counter bits 7–0 |
| $900 | TOD_MID | Event counter bits 15–8 |
| $A00 | TOD_HI | Event counter bits 23–16 |
| $C00 | SDR | Serial data register |
| $D00 | ICR | Interrupt control register |
| $E00 | CRA | Control register A |
| $F00 | CRB | Control register B |

## 8. Kickstart ROM

### Memory Mapping
- ROM location: $FC0000–$FFFFFF (256 KB for Kickstart 1.3+)
- At boot: ROM is overlaid at $000000 (OVL bit set in CIA-A PRA)
- After initialization: software clears OVL, ROM visible only at $FC0000+
- Exception vectors copied to chip RAM at $000000 during boot

### Boot Sequence
1. CPU reads reset vectors from ROM overlay ($000000 = SSP, $000004 = PC)
2. Kickstart initializes hardware (chipset, CIA timers, etc.)
3. Exec library initialized (task scheduler, memory manager, interrupts)
4. Device drivers loaded (trackdisk.device, keyboard.device, etc.)
5. DOS library initialized (AmigaDOS)
6. Boot from DF0: (floppy) — reads bootblock (first 2 sectors, 1024 bytes)
7. If valid bootblock ($444F5300 "DOS\0" magic, checksum valid): execute boot code
8. If no valid boot: display hand/insert-disk animation

### Key ROM Libraries
- **exec.library:** Kernel — task scheduling, interrupts, memory, message passing
- **dos.library:** File system operations, process management
- **graphics.library:** Drawing primitives, display management, sprites
- **intuition.library:** Windows, screens, gadgets (GUI toolkit)
- **layers.library:** Clipping and layered windows

## 9. Memory Map

| Address Range | Size | Description |
|--------------|------|-------------|
| $000000–$07FFFF | 512 KB | Chip RAM (DMA accessible) |
| $080000–$0FFFFF | 512 KB | Chip RAM expansion (ECS Agnus) |
| $100000–$1FFFFF | 1 MB | Extended chip RAM (Fat Agnus) |
| $200000–$9FFFFF | 8 MB | Primary autoconfig (Zorro II) expansion |
| $A00000–$BEFFFF | ~2 MB | Reserved |
| $BF0000–$BFFFFF | 64 KB | CIA space |
| $BFD000 | - | CIA-B (even bytes) |
| $BFE001 | - | CIA-A (odd bytes) |
| $C00000–$C7FFFF | 512 KB | Slow (Ranger) RAM / expansion |
| $C80000–$DBFFFF | ~1.25 MB | Reserved |
| $DC0000–$DCFFFF | 64 KB | Real-time clock |
| $DD0000–$DEFFFF | 128 KB | Reserved |
| $DF0000–$DFFFFF | 64 KB | Custom chip registers |
| $DFF000–$DFF1FE | 510 bytes | Custom chip register space |
| $E00000–$E7FFFF | 512 KB | Reserved (A590 SCSI) |
| $E80000–$EFFFFF | 512 KB | Autoconfig space (expansion board IDs) |
| $F00000–$F7FFFF | 512 KB | Reserved |
| $F80000–$FBFFFF | 256 KB | Reserved (or diagnostic ROM) |
| $FC0000–$FFFFFF | 256 KB | Kickstart ROM |

### Bus Arbitration
- Agnus arbitrates between DMA channels and CPU
- Each scanline divided into slots: even cycles for DMA, odd for CPU (in general)
- CPU is halted during DMA-heavy operations (copper/blitter/bitplane fetches)
- "Bus stealing": Blitter and CPU alternate bus access when both active

## 10. Floppy Disk System

### Physical Format
- 3.5-inch double-density disks
- 80 tracks (cylinders) per side, 2 sides
- 11 sectors per track (AmigaDOS standard), 512 bytes per sector
- Total capacity: 80 × 2 × 11 × 512 = 901,120 bytes = 880 KB

### ADF (Amiga Disk File) Format
- Raw sector dump: 901,120 bytes (exactly 880 KB)
- Track order: track 0 side 0, track 0 side 1, track 1 side 0, track 1 side 1, ...
- Each track: 11 sectors × 512 bytes = 5,632 bytes
- Total: 160 tracks × 5,632 = 901,120 bytes

### MFM Encoding
- Hardware uses Modified Frequency Modulation
- Each data bit encoded as clock+data pair
- Sync words: $4489 marks start of sector
- Sector format: sync, header info, header checksum, data area, data checksum
- Raw MFM track: ~12,800 bytes (encoded) → 5,632 bytes (decoded)

### Track Format (AmigaDOS)
- Gap (optional padding)
- For each of 11 sectors:
  - Sync ($4489 $4489)
  - Header: format byte, track number, sector number, sectors-to-gap
  - Header checksum (XOR of header longs)
  - Data checksum (XOR of data longs)
  - 512 bytes data (MFM encoded)

### Drive Timing
- Disk rotation: 300 RPM (200ms per revolution)
- One track read: ~200ms
- Step time: ~3ms per track
- Seek from track 0 to 79: ~240ms

## 11. Keyboard Interface

- Serial protocol between keyboard controller (6500/1) and CIA-A
- Keyboard sends 8-bit raw keycodes (make/break)
- Bit 7: 0 = key pressed, 1 = key released
- Handshake: CIA-A SP pin, active-low acknowledge pulse
- Key codes: not ASCII — raw scan codes mapped by keymap.library
- Special keys: Caps Lock has LED control (active low)
- Reset: Ctrl+Amiga+Amiga sends reset warning, then triggers hardware reset

## 12. Joystick/Mouse Ports

### Port Addresses
- Port 1 (mouse port): JOY0DAT ($DFF00A), buttons via CIA-A PRA bit 6
- Port 2 (joystick port): JOY1DAT ($DFF00C), buttons via CIA-A PRA bit 7

### Mouse (Quadrature Encoding)
- JOYxDAT bits: Y counter (bits 15–8), X counter (bits 7–0)
- Movement detected by polling counter differences
- Left button: fire button (CIA-A PRA)
- Right button: directly directly directly directly directly directly directly directly directly directly POT register ($DFF012)
- Middle button: directly directly directly POT register

### Joystick
- Digital: up/down/left/right from JOYxDAT bit decoding
- JOYxDAT: V1/V0 in bits 9,8 (vertical), H1/H0 in bits 1,0 (horizontal)
- Direction decoding: up = V1 XOR V0, down = V1, left = H1 XOR H0, right = H1
- Fire button from CIA-A PRA

## 13. Serial and Parallel Ports

### Serial Port
- Built-in RS-232 compatible
- SERDAT ($030): transmit data register
- SERDATR ($018): receive data register + status
- SERPER ($032): baud rate period
- Baud = clock / (period + 1), max ~300 kbaud
- 8/9-bit data, no parity in hardware

### Parallel Port
- 8-bit bidirectional data via CIA-A Port B
- Directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly control lines via CIA-B Port A (BUSY, POUT, SEL)
- Active low STROBE from CIA-A Timer B

## 14. DMA Timing and Bus Cycles

### Horizontal Line Timing (PAL)
- Line length: 227.5 color clocks (CCK) = 455 low-resolution pixels
- Visible display: ~320 pixels (lores), configurable via DIWSTRT/DIWSTOP
- DMA slots allocated per line:
  - Refresh: 4 slots
  - Disk: 3 slots
  - Audio: 4 slots
  - Sprites: 16 slots (2 per sprite)
  - Bitplanes: variable (2 per bitplane in lores, 4 in hires)
  - Copper/Blitter: remaining slots
  - CPU: whatever's left

### Vertical Timing (PAL)
- Total lines: 312 (long frame) / 313 (short frame) for interlace
- Visible lines: 256 (standard), up to 283 (overscan)
- Vertical blank: lines 0–25 approximately
- VBLANK interrupt at line $00

### Color Clock
- 1 color clock = 2 lores pixels = 4 hires pixels
- Frequency: 3.546895 MHz (PAL) / 3.579545 MHz (NTSC)
- CPU clock = 2 × color clock = 7.09 MHz (PAL)
