# Custom Chip Register Quick Reference

## Read-Only Registers ($DFF000+)

| Offset | Name | Description |
|--------|------|-------------|
| $002 | DMACONR | DMA control read |
| $004 | VPOSR | Beam V position (high bits) + LOF |
| $006 | VHPOSR | Beam V (low) and H position |
| $00A | JOY0DAT | Joystick/mouse port 0 |
| $00C | JOY1DAT | Joystick/mouse port 1 |
| $00E | CLXDAT | Collision data (read + clear) |
| $010 | ADKCONR | Audio/disk control read |
| $012 | POT0DAT | Potentiometer port 0 |
| $014 | POT1DAT | Potentiometer port 1 |
| $016 | POTGOR | Pot port data read |
| $018 | SERDATR | Serial data + status |
| $01A | DSKBYTR | Disk byte and status |
| $01C | INTENAR | Interrupt enable read |
| $01E | INTREQR | Interrupt request read |

## Key Write Registers

| Offset | Name | Description |
|--------|------|-------------|
| $020/$022 | DSKPTH/L | Disk DMA pointer |
| $024 | DSKLEN | Disk DMA length + control |
| $040 | BLTCON0 | Blitter control 0 |
| $042 | BLTCON1 | Blitter control 1 |
| $044/$046 | BLTAFWM/BLTALWM | Blitter masks |
| $058 | BLTSIZE | Blitter size (triggers blit) |
| $080/$082 | COP1LCH/L | Copper list 1 pointer |
| $084/$086 | COP2LCH/L | Copper list 2 pointer |
| $088 | COPJMP1 | Copper restart from list 1 |
| $08A | COPJMP2 | Copper restart from list 2 |
| $08E | DIWSTRT | Display window start |
| $090 | DIWSTOP | Display window stop |
| $092 | DDFSTRT | Data fetch start |
| $094 | DDFSTOP | Data fetch stop |
| $096 | DMACON | DMA control (SET/CLR) |
| $09A | INTENA | Interrupt enable (SET/CLR) |
| $09C | INTREQ | Interrupt request (SET/CLR) |
| $09E | ADKCON | Audio/disk control (SET/CLR) |
| $100 | BPLCON0 | Bitplane control 0 |
| $102 | BPLCON1 | Bitplane control 1 (scroll) |
| $104 | BPLCON2 | Bitplane control 2 (priority) |
| $108/$10A | BPL1MOD/BPL2MOD | Bitplane modulo |
| $0E0-$0F6 | BPL1PTH-BPL6PTL | Bitplane pointers |
| $120-$13E | SPR0PTH-SPR7PTL | Sprite pointers |
| $180-$1BE | COLOR00-COLOR31 | Color palette |
