# Phase 4: Custom Chipset — Detailed Task Breakdown

## Task 4.1: Agnus DMA Engine
- [ ] Implement beam counter (H 0-226, V 0-311 PAL)
  - [ ] Horizontal wrap at 227
  - [ ] Vertical wrap at 312 (PAL) / 262 (NTSC)
  - [ ] Long frame toggle for interlace
- [ ] DMA slot allocation per scanline
  - [ ] Fixed slots: refresh (4), disk (3), audio (4), sprites (16)
  - [ ] Variable slots: bitplanes (mode-dependent)
  - [ ] Remaining: Copper, Blitter, CPU
- [ ] DMA channel enable/disable via DMACON
  - [ ] Master enable (bit 9)
  - [ ] Individual channel bits
- [ ] Bitplane DMA
  - [ ] Pointer auto-increment per fetch
  - [ ] Modulo application at end of line
  - [ ] Support 1-6 bitplanes
  - [ ] Lores vs hires fetch timing
- [ ] Sprite DMA
  - [ ] 2 words per sprite per line
  - [ ] Position/control fetch
  - [ ] Data fetch
- [ ] Audio DMA
  - [ ] 1 word per channel per period
  - [ ] Location reload on length exhaustion

## Task 4.2: Denise Video Pipeline
- [ ] Bitplane shift registers
  - [ ] Load from DMA data
  - [ ] Shift one bit per pixel
  - [ ] Scroll delay (BPLCON1)
- [ ] Lores mode
  - [ ] 320 pixels per line
  - [ ] 1-5 bitplanes → 2-32 colors
- [ ] Hires mode
  - [ ] 640 pixels per line
  - [ ] 1-4 bitplanes → 2-16 colors
- [ ] HAM mode
  - [ ] 6 bitplanes: 2 control + 4 data
  - [ ] Control 00: set from palette
  - [ ] Control 01: modify blue
  - [ ] Control 10: modify red
  - [ ] Control 11: modify green
  - [ ] Reset to background at line start
- [ ] EHB mode
  - [ ] 6 bitplanes, no HAM flag
  - [ ] Colors 32-63 = half brightness of 0-31
- [ ] Dual playfield
  - [ ] PF1: bitplanes 0,2,4
  - [ ] PF2: bitplanes 1,3,5
  - [ ] Priority control via BPLCON2
- [ ] Display window
  - [ ] DIWSTRT/DIWSTOP defines visible area
  - [ ] DDFSTRT/DDFSTOP defines data fetch window
- [ ] Color register output
  - [ ] 12-bit RGB → framebuffer conversion

## Task 4.3: Sprite Rendering
- [ ] 8 hardware sprites
  - [ ] 16 pixels wide
  - [ ] 3 colors + transparent
  - [ ] Arbitrary height (start/stop lines)
- [ ] Sprite pair attachment
  - [ ] Pairs: 0-1, 2-3, 4-5, 6-7
  - [ ] Attached: 15 colors (4 bitplanes)
- [ ] Sprite-playfield priority
  - [ ] 4 priority levels via BPLCON2
- [ ] Collision detection
  - [ ] Sprite-sprite collisions
  - [ ] Sprite-playfield collisions
  - [ ] CLXDAT/CLXCON registers
- [ ] Sprite multiplexing
  - [ ] Reposition sprites mid-frame via Copper

## Task 4.4: Paula Audio
- [ ] Channel state machine
  - [ ] Idle → pending → active → idle
  - [ ] DMA fetch trigger
  - [ ] Period counter
- [ ] Sample playback
  - [ ] 8-bit signed PCM
  - [ ] Alternating high/low bytes
  - [ ] Period: min 124 (28.6 kHz)
- [ ] Volume control (0-64)
- [ ] Stereo mixing
  - [ ] Left: channels 0 + 3
  - [ ] Right: channels 1 + 2
- [ ] Audio modulation
  - [ ] Period modulation: ch1→ch0, ch3→ch2
  - [ ] Volume modulation
- [ ] Audio interrupt on length exhaustion

## Task 4.5: Interrupt Controller
- [ ] INTENA register (SET/CLR protocol)
  - [ ] Master enable (bit 14)
  - [ ] Individual source bits (0-13)
- [ ] INTREQ register (SET/CLR protocol)
  - [ ] Software-triggerable
  - [ ] Hardware auto-set on events
- [ ] Priority resolution
  - [ ] Level 6: EXTER (CIA-B)
  - [ ] Level 5: RBF, DSKSYN
  - [ ] Level 4: AUD0-3
  - [ ] Level 3: COPER, VERTB, BLIT
  - [ ] Level 2: PORTS (CIA-A)
  - [ ] Level 1: TBE, DSKBLK, SOFT
- [ ] Integration with 68000 IPL pins
