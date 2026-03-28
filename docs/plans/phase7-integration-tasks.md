# Phase 7: System Integration — Detailed Task Breakdown

## Task 7.1: System Reset
- [ ] Load reset SSP from ROM overlay $000000
- [ ] Load reset PC from ROM overlay $000004
- [ ] Initialize SR to $2700 (supervisor, all masked)
- [ ] Clear OVL after Kickstart copies vectors to chip RAM
  - [ ] Monitor CIA-A port A bit 0 writes
  - [ ] Switch memory map when OVL cleared
- [ ] Initialize chipset to known state
  - [ ] Clear all DMA (DMACON = 0)
  - [ ] Clear all interrupts (INTENA/INTREQ = 0)
  - [ ] Reset beam counters

## Task 7.2: Boot from Floppy
- [ ] Kickstart reads bootblock (track 0, sectors 0-1)
  - [ ] Uses disk DMA to read 1024 bytes
- [ ] Bootblock validation
  - [ ] Check "DOS\0" magic
  - [ ] Verify checksum
- [ ] Bootblock code execution
  - [ ] Jump to bootblock entry point ($0C offset)
  - [ ] Pass exec.library base in A6
- [ ] No-disk behavior
  - [ ] Display insert-disk animation
  - [ ] Wait for disk change interrupt

## Task 7.3: Host Video Output
- [ ] Framebuffer generation
  - [ ] 320×256 lores (PAL)
  - [ ] 640×256 hires (PAL)
  - [ ] 12-bit to 32-bit color conversion
- [ ] SDL2 or platform window
  - [ ] Create window at 2× or 3× scale
  - [ ] Copy framebuffer per frame
  - [ ] Handle window close event
- [ ] VSync timing
  - [ ] Target 50 Hz (PAL)
  - [ ] Sleep/busy-wait to maintain speed

## Task 7.4: Host Audio Output
- [ ] Audio buffer management
  - [ ] Ring buffer for PCM data
  - [ ] 44100 Hz stereo output
- [ ] SDL2 or platform audio
  - [ ] Open audio device
  - [ ] Callback-driven playback
  - [ ] Buffer underrun handling
- [ ] Sample rate conversion
  - [ ] Amiga ~28 kHz → host 44.1 kHz
  - [ ] Linear interpolation

## Task 7.5: Host Input
- [ ] Keyboard mapping
  - [ ] SDL2 scan code → Amiga raw key code
  - [ ] Handle key repeat
  - [ ] Special keys: Amiga, Help, numeric keypad
- [ ] Mouse input
  - [ ] Relative movement → quadrature counters
  - [ ] Button mapping (left, right, middle)
- [ ] Joystick input
  - [ ] Digital direction mapping
  - [ ] Fire button

## Task 7.6: Configuration
- [ ] Command-line arguments
  - [ ] ROM path (required)
  - [ ] Disk path (optional)
  - [ ] PAL/NTSC selection
  - [ ] Chip RAM size (256K, 512K, 1M, 2M)
  - [ ] Slow RAM enable
- [ ] Save states
  - [ ] Serialize full machine state
  - [ ] Quick save/load hotkeys
