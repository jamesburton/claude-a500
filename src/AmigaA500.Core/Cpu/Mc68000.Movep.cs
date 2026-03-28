namespace AmigaA500.Core.Cpu;

public sealed partial class Mc68000
{
    /// <summary>
    /// MOVEP — Move Peripheral Data. Transfers bytes between data register
    /// and alternating memory addresses (even or odd bytes only).
    /// Used for 8-bit peripheral access on 16-bit bus.
    /// </summary>
    internal void ExecuteMovep(ushort opcode)
    {
        int dataReg = (opcode >> 9) & 7;
        int addrReg = opcode & 7;
        short displacement = (short)FetchWord();
        uint addr = (uint)(A[addrReg] + displacement);

        int mode = (opcode >> 6) & 7;

        switch (mode)
        {
            case 4: // MOVEP.W (d16,An),Dn — memory to register, word
                D[dataReg] = (D[dataReg] & 0xFFFF0000) |
                    ((uint)ReadByte(addr) << 8) |
                    ReadByte(addr + 2);
                break;

            case 5: // MOVEP.L (d16,An),Dn — memory to register, long
                D[dataReg] =
                    ((uint)ReadByte(addr) << 24) |
                    ((uint)ReadByte(addr + 2) << 16) |
                    ((uint)ReadByte(addr + 4) << 8) |
                    ReadByte(addr + 6);
                break;

            case 6: // MOVEP.W Dn,(d16,An) — register to memory, word
                WriteByte(addr, (byte)(D[dataReg] >> 8));
                WriteByte(addr + 2, (byte)D[dataReg]);
                break;

            case 7: // MOVEP.L Dn,(d16,An) — register to memory, long
                WriteByte(addr, (byte)(D[dataReg] >> 24));
                WriteByte(addr + 2, (byte)(D[dataReg] >> 16));
                WriteByte(addr + 4, (byte)(D[dataReg] >> 8));
                WriteByte(addr + 6, (byte)D[dataReg]);
                break;
        }
    }
}
