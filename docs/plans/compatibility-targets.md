# Compatibility Targets

## Tier 1 — Must Work (Core Functionality)
- [ ] Boot Kickstart 1.3 ROM to "Insert disk" screen
  - [ ] Memory sizing loop completes
  - [ ] Exec library initializes
  - [ ] Custom chip registers respond
  - [ ] Display shows hand/disk animation
- [ ] Boot from floppy (ADF)
  - [ ] Bootblock read via disk DMA
  - [ ] Bootblock checksum validation
  - [ ] Bootblock code execution

## Tier 2 — Should Work (Basic Software)
- [ ] Workbench 1.3 boot to desktop
  - [ ] Intuition windows render
  - [ ] Mouse pointer visible and responsive
  - [ ] Disk icon appears
  - [ ] CLI opens
- [ ] Simple PD games
  - [ ] SEUCK-made games
  - [ ] AMOS-based games
  - [ ] Lores 5-bitplane display
  - [ ] Joystick input

## Tier 3 — Nice to Have (Advanced)
- [ ] Demoscene productions
  - [ ] Copper effects (color bars, gradients)
  - [ ] Blitter operations (scrolling, bob animation)
  - [ ] Sprite multiplexing
  - [ ] Audio playback (MOD/ProTracker)
- [ ] Complex games
  - [ ] Multi-disk support
  - [ ] Hires display modes
  - [ ] HAM picture display

## Tier 4 — Stretch Goals
- [ ] ECS features (1MB chip RAM, superhires)
- [ ] Hard disk (HDF) support
- [ ] Save states
- [ ] Fast-forward / rewind
