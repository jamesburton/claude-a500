# Phase 1: CPU Core — Detailed Task Breakdown

## Task 1.1: Register File
- [ ] Implement D0-D7 as uint[8]
  - [ ] Byte/word/long read accessors
  - [ ] Byte/word/long write accessors (preserving unmodified portions)
- [ ] Implement A0-A7 as uint[8]
  - [ ] A7 as active stack pointer
  - [ ] USP/SSP switching on S-bit change
- [ ] Implement Status Register
  - [ ] Individual flag properties (C, V, Z, N, X)
  - [ ] Interrupt mask (I0-I2)
  - [ ] Supervisor bit with stack swap
  - [ ] Trace bit

## Task 1.2: Instruction Decoder — Group 0
- [ ] ORI to CCR/SR
  - [ ] Byte size for CCR
  - [ ] Word size for SR (privileged)
- [ ] ANDI to CCR/SR
- [ ] SUBI with flag computation
  - [ ] V flag: source and dest different signs, result different from dest
  - [ ] C flag: borrow detection
  - [ ] X flag: copy of C
- [ ] ADDI with flag computation
- [ ] EORI to CCR/SR
- [ ] CMPI (no writeback)
- [ ] Static bit operations
  - [ ] BTST: Z flag = tested bit
  - [ ] BCHG: toggle bit
  - [ ] BCLR: clear bit
  - [ ] BSET: set bit
  - [ ] Register operand: 32-bit modulo
  - [ ] Memory operand: 8-bit modulo

## Task 1.2b: Instruction Decoder — Groups 1-3 (MOVE)
- [ ] MOVE.B with all source/dest addressing modes
  - [ ] Post-increment adjusts by 1 (2 for A7)
  - [ ] Pre-decrement adjusts by 1 (2 for A7)
- [ ] MOVE.W with all addressing modes
- [ ] MOVE.L with all addressing modes
- [ ] MOVEA.W sign-extends to 32 bits
- [ ] MOVEA.L direct copy
- [ ] Flag setting: N, Z cleared; V, C cleared

## Task 1.2c: Group 4 (Miscellaneous)
- [ ] LEA — all control addressing modes
  - [ ] (An), d16(An), d8(An,Xi), abs.W, abs.L, d16(PC), d8(PC,Xi)
- [ ] PEA — push effective address
- [ ] SWAP — exchange register halves
- [ ] CLR — clear operand (reads then writes on 68000!)
- [ ] NEG / NEGX — negate with/without extend
- [ ] NOT — ones complement
- [ ] TST — test operand
- [ ] EXT.W — sign-extend byte to word
- [ ] EXT.L — sign-extend word to long
- [ ] MOVEM — register save/restore
  - [ ] To memory: normal order or reversed (pre-decrement)
  - [ ] From memory: normal order, post-increment updates An
- [ ] JSR — push PC, jump
- [ ] JMP — jump
- [ ] RTS — pop PC
- [ ] RTE — pop SR and PC
- [ ] TRAP #n — exception vector 32+n
- [ ] LINK / UNLK — frame setup/teardown
- [ ] MOVE to/from SR, CCR, USP
- [ ] NOP, STOP, RESET

## Task 1.2d-1.2h: Remaining Groups
- [ ] Group 5: ADDQ, SUBQ, Scc, DBcc
  - [ ] All 16 condition codes
  - [ ] DBcc: decrement, branch if counter != -1
- [ ] Group 6: Bcc, BSR, BRA
  - [ ] 8-bit and 16-bit displacement
  - [ ] All 14 branch conditions
- [ ] Group 7: MOVEQ (8-bit immediate, sign-extended)
- [ ] Group 8: OR, DIVU, DIVS, SBCD
  - [ ] Division by zero: exception vector 5
  - [ ] Overflow: V flag set, result unchanged
- [ ] Group 9: SUB, SUBA, SUBX
- [ ] Group B: CMP, CMPA, CMPM, EOR
  - [ ] CMP: doesn't affect X flag
- [ ] Group C: AND, MULU, MULS, ABCD, EXG
- [ ] Group D: ADD, ADDA, ADDX
- [ ] Group E: ASL, ASR, LSL, LSR, ROL, ROR, ROXL, ROXR
  - [ ] Register shift: count from Dn & 63
  - [ ] Immediate shift: 1-8 (0 encodes 8)
  - [ ] Memory shift: always 1 bit, word size

## Task 1.3: Exception Handling
- [ ] Exception vector table at $000000-$0003FF
- [ ] Group 0: Reset, Bus Error, Address Error
- [ ] Group 1: Trace
- [ ] Group 2: Interrupt processing
  - [ ] Level comparison with SR mask
  - [ ] Level 7 NMI
  - [ ] Auto-vectoring ($064-$07C)
- [ ] TRAP, Privilege Violation, Illegal Instruction, Line-A/F

## Task 1.4: Cycle Counting
- [ ] Base bus access: 4 cycles per word read/write
- [ ] Effective address calculation cycles
- [ ] MUL: 38-70 cycles depending on operand
- [ ] DIV: 120-158 cycles
- [ ] Internal processing cycles per instruction
