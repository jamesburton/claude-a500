# Phase 3: CIA Chips — Detailed Task Breakdown

## Task 3.1: CIA Core
- [ ] Timer A
  - [ ] Continuous mode: count and reload
  - [ ] One-shot mode: count, stop on underflow
  - [ ] Count phi2 (system clock) mode
  - [ ] Count CNT pin mode
  - [ ] Force load from latch
- [ ] Timer B
  - [ ] Same modes as Timer A
  - [ ] Additional: count Timer A underflows
- [ ] TOD counter
  - [ ] 24-bit event counter (50/60 Hz input)
  - [ ] Latching on high-byte read
  - [ ] Un-latching on low-byte read
  - [ ] Alarm comparison and interrupt
  - [ ] Write to alarm vs counter (CRB bit 7)
- [ ] ICR (Interrupt Control Register)
  - [ ] SET/CLR protocol on write
  - [ ] Read-and-clear semantics
  - [ ] IR bit (bit 7) in read data
  - [ ] 5 interrupt sources: TimerA, TimerB, TOD alarm, serial, FLAG
- [ ] Port I/O
  - [ ] Data direction registers (DDRA, DDRB)
  - [ ] Output bits from PRA/PRB
  - [ ] Input bits from external signals
  - [ ] Combined read: (PRA & DDR) | (external & ~DDR)
- [ ] Serial data register
  - [ ] 8-bit shift register
  - [ ] Trigger interrupt on completion

## Task 3.2: CIA-A Integration
- [ ] Keyboard serial interface
  - [ ] Receive inverted, rotated scan codes
  - [ ] Trigger serial port interrupt on key event
  - [ ] Handshake protocol
- [ ] Fire button inputs
  - [ ] Port 1 button via PRA bit 6
  - [ ] Port 2 button via PRA bit 7
- [ ] Disk drive sensors
  - [ ] /RDY (bit 5): drive ready
  - [ ] /TK0 (bit 4): track zero
  - [ ] /WPRO (bit 3): write protect
  - [ ] /CHNG (bit 2): disk changed
- [ ] LED and OVL
  - [ ] Power LED control (bit 1)
  - [ ] Kickstart overlay (bit 0)
- [ ] Interrupt output → PORTS (IPL level 2)

## Task 3.3: CIA-B Integration
- [ ] Disk drive control
  - [ ] /MTR (bit 7): motor on/off
  - [ ] /SEL0-3 (bits 3-6): drive select
  - [ ] /SIDE (bit 2): head select
  - [ ] DIR (bit 1): step direction
  - [ ] /STEP (bit 0): step pulse
- [ ] Serial port control lines
  - [ ] /DTR, /RTS, /CD, /CTS, /DSR
- [ ] Parallel port signals
  - [ ] SEL, POUT, BUSY
- [ ] Interrupt output → EXTER (IPL level 6)

# Phase 5: Blitter & Copper — Detailed Task Breakdown

## Task 5.1: Blitter Area Mode
- [ ] Channel management
  - [ ] Enable/disable A, B, C, D independently
  - [ ] DMA pointer setup and auto-advance
  - [ ] Modulo application per row
- [ ] Barrel shifter
  - [ ] Channel A: 0-15 bit shift right
  - [ ] Channel B: 0-15 bit shift right
  - [ ] Pipeline previous/current word pairs
- [ ] Masking
  - [ ] First word mask (BLTAFWM)
  - [ ] Last word mask (BLTALWM)
  - [ ] Applied to channel A only
- [ ] Minterm logic
  - [ ] All 256 boolean functions of (A, B, C)
  - [ ] Common patterns: copy ($F0), cookie-cut ($CA), invert ($0F)
- [ ] Direction
  - [ ] Ascending (left-to-right, top-to-bottom)
  - [ ] Descending (right-to-left, bottom-to-top)
- [ ] Size and trigger
  - [ ] BLTSIZE: height (10 bits) × width (6 bits)
  - [ ] Write triggers operation
  - [ ] Zero flag tracking
  - [ ] Completion interrupt (BLIT)

## Task 5.2: Blitter Fill Mode
- [ ] Inclusive fill
  - [ ] Toggle fill state on each set bit
  - [ ] Fill between edges
- [ ] Exclusive fill
  - [ ] Toggle on edge transitions only
- [ ] Fill carry input (FCI bit)
- [ ] Process right-to-left within words

## Task 5.3: Blitter Line Mode
- [ ] Bresenham algorithm
  - [ ] 8 octant selection
  - [ ] Error accumulator in APt
  - [ ] Step increments in AMod/BMod
- [ ] Textured lines
  - [ ] Pattern from channel A data
  - [ ] 16-bit repeating pattern
- [ ] Single-pixel output
- [ ] OR mode (preserve existing pixels)

## Task 5.4: Copper
- [ ] MOVE instruction
  - [ ] Write value to chipset register
  - [ ] Dangerous register protection
  - [ ] COPCON bit for low register access
- [ ] WAIT instruction
  - [ ] Beam position comparison
  - [ ] VP/HP masks for flexible matching
  - [ ] BFD (blitter-finish-disable)
  - [ ] $FFFF,$FFFE = wait forever (end of list)
- [ ] SKIP instruction
  - [ ] Same as WAIT but skips next instruction
  - [ ] Identified by bit 0 of word 2
- [ ] List management
  - [ ] COP1LC / COP2LC pointers
  - [ ] COPJMP1 / COPJMP2 restart strobes
  - [ ] Automatic restart on VBLANK
