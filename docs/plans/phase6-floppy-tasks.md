# Phase 6: Floppy Disk — Detailed Task Breakdown

## Task 6.1: ADF File Support
- [ ] Load 880 KB raw sector dump
  - [ ] Validate file size (901,120 bytes)
  - [ ] Support both OFS and FFS disk types
- [ ] Track/sector addressing
  - [ ] 80 cylinders × 2 sides × 11 sectors × 512 bytes
  - [ ] ReadTrack(cylinder, side) → 5632 bytes
  - [ ] ReadSector(cylinder, side, sector) → 512 bytes
- [ ] Bootblock validation
  - [ ] "DOS\0" magic at offset 0
  - [ ] Checksum verification (carry-add all longs)
- [ ] Read-write support
  - [ ] WriteTrack for save-game support
  - [ ] WriteSector for file operations
- [ ] Multiple drives
  - [ ] DF0-DF3 support
  - [ ] Individual disk insertion/ejection

## Task 6.2: MFM Encoding
- [ ] MFM clock bit insertion
  - [ ] Clock = NOT(previous_data OR next_data)
  - [ ] Produces 2 bits per data bit
- [ ] MFM decoding
  - [ ] Strip clock bits to recover data
  - [ ] Handle sync word detection ($4489)
- [ ] Sector header generation
  - [ ] Format byte, track, sector, sectors-to-gap
  - [ ] Header checksum (XOR of header longs)
- [ ] Data checksum
  - [ ] XOR of all data longs
  - [ ] Validation on read

## Task 6.3: Drive Emulation
- [ ] Head stepping
  - [ ] Step toward center (cylinder++)
  - [ ] Step toward edge (cylinder--)
  - [ ] Track 0 sensor
  - [ ] Cannot step below 0 or above 79
  - [ ] Step time: ~3ms
- [ ] Side selection
  - [ ] Upper (0) / lower (1) head
  - [ ] CIA-B port B bit 2
- [ ] Motor control
  - [ ] Motor on/off via CIA-B port B bit 7
  - [ ] Spin-up delay
  - [ ] Ready signal
- [ ] Disk change detection
  - [ ] Change line on insert/eject
  - [ ] CIA-A port A bit 2
- [ ] Write protection
  - [ ] WP sensor via CIA-A port A bit 3

## Task 6.4: Disk DMA
- [ ] DSKPT register (chip RAM destination)
- [ ] DSKLEN register
  - [ ] Double-write trigger
  - [ ] Direction bit (read/write)
  - [ ] Length in words
- [ ] Sync word detection
  - [ ] DSKSYNC register ($4489 default)
  - [ ] DMA paused until sync found
- [ ] DMA transfer
  - [ ] Sequential word transfer to chip RAM
  - [ ] DSKBLK interrupt on completion
- [ ] ADKCON integration
  - [ ] MFM/GCR mode selection
  - [ ] Precompensation settings
