# Timing Reference — Amiga 500 PAL/NTSC

## Clock Frequencies

| Clock | PAL | NTSC |
|-------|-----|------|
| Color clock (CCK) | 3.546895 MHz | 3.579545 MHz |
| CPU clock (2×CCK) | 7.093790 MHz | 7.159090 MHz |
| E-clock (CPU÷10) | 709.379 kHz | 715.909 kHz |

## Frame Timing

| Parameter | PAL | NTSC |
|-----------|-----|------|
| Lines per frame | 312/313 (interlace) | 262/263 |
| Color clocks per line | 227.5 | 227.5 |
| CPU cycles per line | 455 | 455 |
| Frame rate | 50.0 Hz | 59.94 Hz |
| CPU cycles per frame | 141,960 | 119,665 |
| VBLANK lines | ~26 | ~22 |
| Visible lines | ~256 | ~200 |

## DMA Slot Allocation Per Line

| Slot Range (CCK) | Owner | Count |
|-------------------|-------|-------|
| 0-3 | Memory refresh | 4 |
| 4-6 | Disk DMA | 3 |
| 7-10 | Audio DMA (4 channels) | 4 |
| 11-26 | Sprite DMA (8 sprites × 2) | 16 |
| 27-E2 | Bitplane DMA (variable) | varies |
| Remaining | Copper/Blitter/CPU | varies |

## Bitplane DMA Cycles

| Mode | Bitplanes | DMA cycles per line |
|------|-----------|-------------------|
| Lores 1bpl | 1 | 20 |
| Lores 2bpl | 2 | 40 |
| Lores 3bpl | 3 | 60 |
| Lores 4bpl | 4 | 80 |
| Lores 5bpl | 5 | 100 |
| Lores 6bpl (HAM/EHB) | 6 | 120 |
| Hires 1bpl | 1 | 40 |
| Hires 2bpl | 2 | 80 |
| Hires 3bpl | 3 | 120 |
| Hires 4bpl | 4 | 160 |

## CPU Cycles Available Per Line

| Configuration | CPU cycles available |
|--------------|---------------------|
| No DMA | ~440 |
| Lores 5bpl + sprites + audio | ~150 |
| Hires 4bpl + sprites + audio | ~70 |
| Lores 5bpl + blitter nasty | ~0 |

## Audio Timing

| Sample rate | Period value |
|-------------|-------------|
| 28.6 kHz (max) | 124 |
| 22.0 kHz | 161 |
| 11.0 kHz | 322 |
| 8.3 kHz | 428 |
| 4.1 kHz | 856 |

## CIA Timer Resolution

| Timer source | Clock | Resolution |
|-------------|-------|------------|
| Timer A/B phi2 | E-clock (~709 kHz) | 1.41 µs |
| Timer B count A | Timer A rate | Variable |
| TOD | 50/60 Hz | 20/16.7 ms |

## Floppy Timing

| Operation | Duration |
|-----------|----------|
| Disk rotation | 200 ms (300 RPM) |
| Track read | ~200 ms |
| Head step | ~3 ms |
| Full seek (0→79) | ~240 ms |
| Motor spin-up | ~500 ms |
| DMA transfer (1 track) | ~200 ms |
