# Amiga 500 Emulator — Implementation Roadmap

## Phase 1: CPU Core (68000)

### Task 1.1: Register File and State
- [ ] Define Cpu68000 class with D0-D7, A0-A7, PC, SR, USP, SSP
- [ ] Implement SR flag accessors (C, V, Z, N, X, S, T, IPL mask)
- [ ] Implement stack pointer switching on S-bit change
- [ ] Unit tests for register state management

### Task 1.2: Instruction Decoder
- [ ] Build 65536-entry opcode dispatch table
- [ ] Implement group 0 (immediate/bit operations)
  - [ ] ORI, ANDI, SUBI, ADDI, EORI, CMPI
  - [ ] BTST, BSET, BCLR, BCHG
  - [ ] MOVEP
- [ ] Implement group 1-3 (MOVE.B/L/W)
  - [ ] All 14 addressing modes for source
  - [ ] All destination modes
  - [ ] MOVEA variants
- [ ] Implement group 4 (miscellaneous)
  - [ ] LEA, PEA
  - [ ] CLR, NEG, NEGX, NOT
  - [ ] TST, TAS
  - [ ] SWAP, EXT
  - [ ] MOVEM (register save/restore)
  - [ ] JSR, JMP
  - [ ] CHK
  - [ ] TRAP, TRAPV
  - [ ] LINK, UNLK
  - [ ] RTS, RTE, RTR
  - [ ] MOVE to/from SR, USP
  - [ ] NOP, RESET, STOP
- [ ] Implement group 5 (ADDQ/SUBQ/Scc/DBcc)
  - [ ] ADDQ, SUBQ (all sizes)
  - [ ] Scc (all 16 conditions)
  - [ ] DBcc (all 16 conditions)
- [ ] Implement group 6 (Bcc/BSR/BRA)
  - [ ] BRA, BSR
  - [ ] All 14 conditional branches (8-bit and 16-bit displacement)
- [ ] Implement group 7 (MOVEQ)
- [ ] Implement group 8 (OR/DIV/SBCD)
  - [ ] OR (all modes)
  - [ ] DIVU, DIVS
  - [ ] SBCD
- [ ] Implement group 9 (SUB/SUBX)
  - [ ] SUB, SUBA (all sizes)
  - [ ] SUBX (register and memory)
- [ ] Implement group B (CMP/EOR)
  - [ ] CMP, CMPA (all sizes)
  - [ ] CMPM
  - [ ] EOR
- [ ] Implement group C (AND/MUL/ABCD/EXG)
  - [ ] AND (all modes)
  - [ ] MULU, MULS
  - [ ] ABCD
  - [ ] EXG (Dn↔Dn, An↔An, Dn↔An)
- [ ] Implement group D (ADD/ADDX)
  - [ ] ADD, ADDA (all sizes)
  - [ ] ADDX (register and memory)
- [ ] Implement group E (shift/rotate)
  - [ ] ASL, ASR, LSL, LSR, ROL, ROR, ROXL, ROXR
  - [ ] Register and memory variants
  - [ ] Immediate and register shift counts
- [ ] Implement line-A and line-F trap handling

### Task 1.3: Addressing Mode Engine
- [ ] Data register direct (mode 000)
- [ ] Address register direct (mode 001)
- [ ] Address register indirect (mode 010)
- [ ] Post-increment (mode 011) with size-correct increment
- [ ] Pre-decrement (mode 100) with size-correct decrement
- [ ] Displacement d16(An) (mode 101)
- [ ] Indexed d8(An,Xn) (mode 110)
- [ ] Absolute short (mode 111/000)
- [ ] Absolute long (mode 111/001)
- [ ] PC-relative displacement (mode 111/010)
- [ ] PC-relative indexed (mode 111/011)
- [ ] Immediate (mode 111/100)

### Task 1.4: Exception Handling
- [ ] Vector table read
- [ ] Exception processing sequence (save SR, push PC, push SR, load vector)
- [ ] Bus error and address error (group 0)
- [ ] TRAP instruction exceptions
- [ ] Interrupt processing with level comparison
- [ ] Trace exception
- [ ] Privilege violation
- [ ] Illegal instruction (including line-A/F)

### Task 1.5: Cycle Counting
- [ ] Bus access cycle tracking (4 cycles per word access)
- [ ] Internal operation cycles
- [ ] Prefetch queue simulation
- [ ] Instruction-accurate cycle totals for all opcodes
- [ ] Multiply/divide variable timing

## Phase 2: Memory Subsystem

### Task 2.1: Address Bus
- [ ] Implement AddressBus class with 24-bit address space
- [ ] Chip RAM (512 KB, byte/word/long read/write)
- [ ] ROM overlay logic (OVL bit)
- [ ] Unmapped address handling

### Task 2.2: Kickstart ROM
- [ ] ROM file loader (256 KB and 512 KB support)
- [ ] ROM overlay at $000000 during reset
- [ ] ROM checksum validation
- [ ] ROM version detection (1.2, 1.3, 2.x, 3.x)

### Task 2.3: Custom Chip Register Map
- [ ] Register read dispatcher ($DFF000-$DFF1FE)
- [ ] Register write dispatcher
- [ ] SET/CLR protocol for DMACON, INTENA, INTREQ, ADKCON
- [ ] Read-only vs write-only register enforcement
- [ ] Strobe registers (COPJMP1/2, BLTSIZE, etc.)

### Task 2.4: Expansion Memory
- [ ] Slow RAM at $C00000 (512 KB)
- [ ] Fast RAM autoconfig (Zorro II at $200000+)

## Phase 3: CIA Chips

### Task 3.1: CIA Core
- [ ] Timer A implementation (one-shot/continuous, phi2/CNT counting)
- [ ] Timer B implementation (+ count Timer A underflows mode)
- [ ] TOD counter (50/60 Hz input, alarm compare)
- [ ] ICR interrupt control (SET/CLR, read-and-clear)
- [ ] Port I/O with data direction registers
- [ ] Serial data register

### Task 3.2: CIA-A Integration
- [ ] Keyboard interface (serial data receive, handshake)
- [ ] Fire button inputs (joystick/mouse port 1 and 2)
- [ ] Disk sensor inputs (RDY, TK0, WPRO, CHNG)
- [ ] LED control (accent color)
- [ ] OVL bit → memory overlay control
- [ ] Interrupt output → PORTS (level 2)

### Task 3.3: CIA-B Integration
- [ ] Disk drive control (motor, select, side, step, direction)
- [ ] Serial port control lines
- [ ] Parallel port signals
- [ ] Interrupt output → EXTER (level 6)

## Phase 4: Custom Chipset Core

### Task 4.1: Agnus — DMA Engine
- [ ] Beam counter (horizontal 0-227, vertical 0-312 PAL)
- [ ] DMA slot allocation per scanline
- [ ] DMA channel priority arbiter
- [ ] DMACON register handling
- [ ] Bitplane DMA pointer management (auto-increment, modulos)
- [ ] Sprite DMA pointer management
- [ ] Audio DMA pointer management
- [ ] Disk DMA pointer management

### Task 4.2: Denise — Video
- [ ] Bitplane shift register pipeline
- [ ] Lores mode (320×256, 5 bitplanes, 32 colors)
- [ ] Hires mode (640×256, 4 bitplanes, 16 colors)
- [ ] HAM mode (Hold-And-Modify, 4096 colors)
- [ ] EHB mode (Extra Half-Brite, 64 colors)
- [ ] Dual playfield mode
- [ ] Color register management (32 × 12-bit RGB)
- [ ] Display window (DIWSTRT/DIWSTOP)
- [ ] Scrolling (BPLCON1 delays)
- [ ] Framebuffer output (convert 12-bit to 32-bit RGBA)

### Task 4.3: Denise — Sprites
- [ ] Sprite DMA data fetch
- [ ] Sprite position/control registers
- [ ] Sprite rendering (16 pixels wide, 3 colors)
- [ ] Sprite pair attachment (15 colors)
- [ ] Sprite-playfield priority (BPLCON2)
- [ ] Collision detection (CLXDAT/CLXCON)

### Task 4.4: Paula — Audio
- [ ] Audio channel state machine (idle/pending/active)
- [ ] DMA sample fetch
- [ ] Period counter and sample playback
- [ ] Volume control (0-64)
- [ ] Channel mixing (left: ch0+ch3, right: ch1+ch2)
- [ ] Audio modulation (period and volume)
- [ ] PCM output buffer for host audio

### Task 4.5: Paula — Interrupt Controller
- [ ] INTENA/INTREQ registers with SET/CLR
- [ ] Interrupt source to 68000 level mapping
- [ ] Master enable bit
- [ ] Level priority resolution

## Phase 5: Blitter and Copper

### Task 5.1: Blitter — Area Mode
- [ ] Channel A/B/C/D DMA management
- [ ] Barrel shifting for channels A and B
- [ ] First/last word masking
- [ ] Minterm logic (all 256 combinations)
- [ ] Modulo handling (ascending and descending)
- [ ] Zero flag tracking
- [ ] Blit-size trigger and completion interrupt

### Task 5.2: Blitter — Fill Mode
- [ ] Inclusive fill
- [ ] Exclusive fill
- [ ] Fill direction (right-to-left within words)

### Task 5.3: Blitter — Line Mode
- [ ] Bresenham line drawing
- [ ] Octant selection
- [ ] Textured lines (pattern from channel A)
- [ ] Single-pixel vs patterned output

### Task 5.4: Copper
- [ ] MOVE instruction execution
- [ ] WAIT instruction (beam position comparison with masks)
- [ ] SKIP instruction
- [ ] Copper list pointer management (COP1LC/COP2LC)
- [ ] VBLANK restart
- [ ] Copper DMA cycle allocation
- [ ] Dangerous register protection (COPCON)

## Phase 6: Floppy Disk System

### Task 6.1: ADF File Support
- [ ] ADF file loader (880 KB raw sector dump)
- [ ] Track/sector addressing
- [ ] Read-only and read-write support
- [ ] Multiple drive support (DF0-DF3)

### Task 6.2: MFM Encoding
- [ ] MFM clock bit insertion
- [ ] MFM decoding (raw → data)
- [ ] Sector header generation
- [ ] Checksum calculation and validation

### Task 6.3: Drive Emulation
- [ ] Head stepping (track to track)
- [ ] Side selection
- [ ] Motor control
- [ ] Track read timing (200ms rotation)
- [ ] Disk change detection

### Task 6.4: Disk DMA
- [ ] DSKPT/DSKLEN register handling
- [ ] Sync word detection ($4489)
- [ ] DMA transfer to chip RAM
- [ ] DSKBLK interrupt on completion

## Phase 7: Boot Sequence and Integration

### Task 7.1: System Reset
- [ ] Load reset vectors from ROM overlay
- [ ] Initialize CPU state (SSP from vector 0, PC from vector 1)
- [ ] Clear OVL after Kickstart init
- [ ] Kickstart ROM execution

### Task 7.2: Boot from Floppy
- [ ] Bootblock read (track 0, sectors 0-1)
- [ ] Bootblock validation (magic number, checksum)
- [ ] Bootblock code execution

### Task 7.3: Host Integration
- [ ] Main emulation loop with cycle-level stepping
- [ ] Video output (framebuffer → window)
- [ ] Audio output (PCM buffer → audio device)
- [ ] Keyboard input mapping
- [ ] Mouse input (relative movement)
- [ ] Joystick input

## Phase 8: Testing and Compatibility

### Task 8.1: CPU Tests
- [ ] Run standard 68000 test suites
- [ ] Verify all instruction flag behavior
- [ ] Timing accuracy tests

### Task 8.2: Chipset Tests
- [ ] Blitter operation verification
- [ ] Copper list execution tests
- [ ] Display mode rendering tests
- [ ] Audio playback tests

### Task 8.3: ADF Compatibility
- [ ] Boot Workbench disk
- [ ] Run demo disks
- [ ] Run game disks
- [ ] Verify audio/video output quality
