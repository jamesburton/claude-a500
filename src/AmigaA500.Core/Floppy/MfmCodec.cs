namespace AmigaA500.Core.Floppy;

/// <summary>
/// MFM (Modified Frequency Modulation) encoder/decoder for Amiga floppy disks.
/// </summary>
public static class MfmCodec
{
    /// <summary>
    /// Encode a data byte array into MFM. Each data byte produces 2 MFM bytes.
    /// </summary>
    public static byte[] Encode(ReadOnlySpan<byte> data)
    {
        var mfm = new byte[data.Length * 2];
        bool prevBit = false;

        for (int i = 0; i < data.Length; i++)
        {
            ushort encoded = 0;
            byte b = data[i];

            for (int bit = 7; bit >= 0; bit--)
            {
                bool dataBit = (b & (1 << bit)) != 0;
                bool clockBit = !prevBit && !dataBit;

                encoded <<= 2;
                if (clockBit) encoded |= 2;
                if (dataBit) encoded |= 1;

                prevBit = dataBit;
            }

            mfm[i * 2] = (byte)(encoded >> 8);
            mfm[i * 2 + 1] = (byte)(encoded & 0xFF);
        }

        return mfm;
    }

    /// <summary>
    /// Decode MFM data back to original bytes.
    /// </summary>
    public static byte[] Decode(ReadOnlySpan<byte> mfm)
    {
        var data = new byte[mfm.Length / 2];

        for (int i = 0; i < data.Length; i++)
        {
            ushort encoded = (ushort)(mfm[i * 2] << 8 | mfm[i * 2 + 1]);
            byte decoded = 0;

            for (int bit = 7; bit >= 0; bit--)
            {
                // Data bits are at odd positions (bit 0, 2, 4, ...)
                if ((encoded & (1 << (bit * 2))) != 0)
                    decoded |= (byte)(1 << bit);
            }

            data[i] = decoded;
        }

        return data;
    }

    /// <summary>
    /// Encode an Amiga track sector for MFM raw track format.
    /// </summary>
    public static ushort[] EncodeSector(byte[] sectorData, int track, int sector, int sectorsToGap)
    {
        var words = new List<ushort>();

        // Sync words
        words.Add(0x4489);
        words.Add(0x4489);

        // Header: format byte (0xFF = AmigaDOS), track, sector, sectors-to-gap
        byte[] header = { 0xFF, (byte)track, (byte)sector, (byte)sectorsToGap };

        // Encode header (odd/even split MFM)
        uint headerLong = (uint)(header[0] << 24 | header[1] << 16 | header[2] << 8 | header[3]);
        words.Add((ushort)(headerLong >> 16));
        words.Add((ushort)(headerLong & 0xFFFF));

        // Header checksum (XOR of header longs)
        words.Add((ushort)(headerLong >> 16));
        words.Add((ushort)(headerLong & 0xFFFF));

        // Data (512 bytes = 256 words)
        for (int i = 0; i < sectorData.Length; i += 2)
        {
            words.Add((ushort)(sectorData[i] << 8 | sectorData[i + 1]));
        }

        // Data checksum
        uint dataChecksum = 0;
        for (int i = 0; i < sectorData.Length; i += 4)
        {
            uint word = (uint)(sectorData[i] << 24 | sectorData[i + 1] << 16 |
                              sectorData[i + 2] << 8 | sectorData[i + 3]);
            dataChecksum ^= word;
        }
        words.Add((ushort)(dataChecksum >> 16));
        words.Add((ushort)(dataChecksum & 0xFFFF));

        return words.ToArray();
    }

    /// <summary>
    /// MFM sync word — marks start of sector.
    /// </summary>
    public const ushort SyncWord = 0x4489;
}
