# Amiga 500 Emulator — Sizing Estimates

Approximate development complexity and size estimates for each emulation subsystem.

## CPU: Motorola 68000

| Component | Estimated LOC | Estimated Classes | Complexity |
|-----------|---------------|-------------------|------------|
| Instruction decoder | 2500 lines | 3 classes | High |
| ALU operations | 800 lines | 1 classes | Medium |
| Address mode resolution | 600 lines | 2 classes | Medium |
| Exception/interrupt handling | 400 lines | 1 classes | High |
| Prefetch/pipeline simulation | 200 lines | 1 classes | Medium |
| Condition code evaluation | 300 lines | 1 classes | Low |
| Disassembler (debug tool) | 1000 lines | 2 classes | Medium |
| **Subtotal** | **5800 lines** | **11 classes** | |

## Agnus (DMA Controller)

| Component | Estimated LOC | Estimated Classes | Complexity |
|-----------|---------------|-------------------|------------|
| DMA scheduler/arbiter | 600 lines | 2 classes | High |
| Beam counter | 200 lines | 1 classes | Medium |
| Register interface | 300 lines | 1 classes | Low |
| Chip RAM controller | 150 lines | 1 classes | Low |
| **Subtotal** | **1250 lines** | **5 classes** | |

## Blitter

| Component | Estimated LOC | Estimated Classes | Complexity |
|-----------|---------------|-------------------|------------|
| Area blit engine | 500 lines | 2 classes | High |
| Line draw engine | 300 lines | 1 classes | Medium |
| Fill mode | 200 lines | 1 classes | Medium |
| Minterm logic | 100 lines | 1 classes | Low |
| Barrel shifter/masks | 150 lines | 1 classes | Low |
| **Subtotal** | **1250 lines** | **6 classes** | |

## Copper

| Component | Estimated LOC | Estimated Classes | Complexity |
|-----------|---------------|-------------------|------------|
| Instruction fetch/decode | 200 lines | 1 classes | Medium |
| WAIT/SKIP logic | 150 lines | 1 classes | Medium |
| MOVE execution | 100 lines | 1 classes | Low |
| Copper list management | 100 lines | 1 classes | Low |
| **Subtotal** | **550 lines** | **4 classes** | |

## Denise (Video)

| Component | Estimated LOC | Estimated Classes | Complexity |
|-----------|---------------|-------------------|------------|
| Bitplane compositor | 600 lines | 2 classes | High |
| Sprite engine | 500 lines | 2 classes | Medium |
| Color register/palette | 200 lines | 1 classes | Low |
| Display modes (HAM, EHB, dual-playfield) | 400 lines | 2 classes | High |
| Collision detection | 200 lines | 1 classes | Medium |
| Video output/framebuffer | 300 lines | 1 classes | Medium |
| **Subtotal** | **2200 lines** | **9 classes** | |

## Paula (Audio + Interrupts)

| Component | Estimated LOC | Estimated Classes | Complexity |
|-----------|---------------|-------------------|------------|
| Audio channel state machine | 400 lines | 2 classes | Medium |
| Audio DMA/mixing | 300 lines | 1 classes | Medium |
| Interrupt controller | 300 lines | 1 classes | Medium |
| Volume/period modulation | 150 lines | 1 classes | Medium |
| Audio output (PCM buffer) | 200 lines | 1 classes | Low |
| **Subtotal** | **1350 lines** | **6 classes** | |

## Floppy Disk Controller

| Component | Estimated LOC | Estimated Classes | Complexity |
|-----------|---------------|-------------------|------------|
| ADF file reader | 300 lines | 2 classes | Low |
| MFM encoder/decoder | 400 lines | 1 classes | Medium |
| Track buffer / DMA | 200 lines | 1 classes | Medium |
| Drive state machine | 300 lines | 1 classes | Medium |
| Sector parser | 250 lines | 1 classes | Medium |
| **Subtotal** | **1450 lines** | **6 classes** | |

## CIA Chips (8520 × 2)

| Component | Estimated LOC | Estimated Classes | Complexity |
|-----------|---------------|-------------------|------------|
| Timer A/B | 300 lines | 1 classes | Medium |
| TOD counter | 150 lines | 1 classes | Low |
| Port I/O (keyboard, disk, serial) | 300 lines | 1 classes | Medium |
| Interrupt control (ICR) | 200 lines | 1 classes | Medium |
| Serial data register | 100 lines | 1 classes | Low |
| **Subtotal** | **1050 lines** | **5 classes** | |

## Memory Subsystem

| Component | Estimated LOC | Estimated Classes | Complexity |
|-----------|---------------|-------------------|------------|
| Address decoder/bus | 400 lines | 2 classes | Medium |
| Chip RAM (512 KB) | 100 lines | 1 classes | Low |
| Slow RAM / expansion | 100 lines | 1 classes | Low |
| Custom chip register map | 500 lines | 2 classes | Medium |
| ROM loader (Kickstart) | 200 lines | 1 classes | Low |
| **Subtotal** | **1300 lines** | **7 classes** | |

## Keyboard Interface

| Component | Estimated LOC | Estimated Classes | Complexity |
|-----------|---------------|-------------------|------------|
| Scan code handler | 200 lines | 1 classes | Low |
| Serial protocol | 150 lines | 1 classes | Medium |
| Keymap translation | 200 lines | 1 classes | Low |
| **Subtotal** | **550 lines** | **3 classes** | |

## Joystick/Mouse Input

| Component | Estimated LOC | Estimated Classes | Complexity |
|-----------|---------------|-------------------|------------|
| Mouse quadrature decoder | 150 lines | 1 classes | Medium |
| Joystick digital input | 100 lines | 1 classes | Low |
| Button handling | 50 lines | 1 classes | Low |
| **Subtotal** | **300 lines** | **3 classes** | |

## Host Platform Integration

| Component | Estimated LOC | Estimated Classes | Complexity |
|-----------|---------------|-------------------|------------|
| SDL2/OpenGL video output | 400 lines | 2 classes | Medium |
| SDL2 audio output | 250 lines | 1 classes | Medium |
| Input mapping (keyboard/gamepad) | 300 lines | 1 classes | Low |
| Main loop / timing sync | 300 lines | 1 classes | Medium |
| Configuration/CLI | 200 lines | 1 classes | Low |
| **Subtotal** | **1450 lines** | **6 classes** | |

## Testing & Debug Tools

| Component | Estimated LOC | Estimated Classes | Complexity |
|-----------|---------------|-------------------|------------|
| CPU instruction tests | 3000 lines | 5 files | Medium |
| Chipset register tests | 1000 lines | 3 files | Medium |
| Integration test harness | 500 lines | 2 files | Medium |
| Disassembler/debugger | 1000 lines | 3 classes | Medium |
| **Subtotal** | **5500 lines** | **13 files** | |

## Grand Total

| Category | LOC | Classes/Files |
|----------|-----|---------------|
| Core emulation | 17,000 lines | 65 classes |
| Host integration | 1,450 lines | 6 classes |
| Tests & tools | 5,500 lines | 13 files |
| **Total** | **~24,000 lines** | **~84 files** |

## Development Phase Estimates

| Phase | KB of code | Key deliverables |
|-------|-----------|-----------------|
| Phase 1: CPU core | 40 KB | Working 68000 with instruction tests |
| Phase 2: Memory & bus | 15 KB | Address decoding, chip RAM, ROM loading |
| Phase 3: CIA chips | 10 KB | Timers, keyboard, disk drive control |
| Phase 4: Custom chipset | 35 KB | Agnus DMA, Denise video, Paula audio |
| Phase 5: Blitter & Copper | 15 KB | 2D graphics acceleration, display lists |
| Phase 6: Floppy & ADF | 12 KB | Disk I/O, ADF loading, boot sequence |
| Phase 7: Host integration | 10 KB | Video/audio output, input handling |
| Phase 8: Testing & polish | 20 KB | Comprehensive tests, compatibility fixes |
