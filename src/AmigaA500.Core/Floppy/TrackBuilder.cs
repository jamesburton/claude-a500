namespace AmigaA500.Core.Floppy;

/// <summary>
/// Builds raw MFM tracks from AmigaDOS sector data for floppy write support.
/// An Amiga track consists of a gap of $AAAA words followed by 11 encoded sectors,
/// each preceded by two $4489 sync words.
/// </summary>
public sealed class TrackBuilder
{
    // Raw track length in bytes for a standard DD floppy at normal motor speed
    public const int RawTrackBytes = 12800;
    public const int SectorsPerTrack = AdfDisk.SectorsPerTrack;
    public const int BytesPerSector = AdfDisk.BytesPerSector;

    // Gap filler word between sectors and at track start
    private const ushort GapWord = 0xAAAA;
    // MFM sync word
    private const ushort SyncWord = MfmCodec.SyncWord;

    /// <summary>
    /// Build a complete raw MFM track from 11 sectors of 512 bytes each.
    /// </summary>
    /// <param name="trackData">
    /// Buffer of exactly <see cref="SectorsPerTrack"/> × <see cref="BytesPerSector"/> = 5632 bytes.
    /// </param>
    /// <param name="cylinder">Cylinder number (0–79).</param>
    /// <param name="side">Side number (0 or 1).</param>
    /// <returns>Raw MFM track bytes, suitable for writing back to a physical disk image.</returns>
    public static byte[] Build(ReadOnlySpan<byte> trackData, int cylinder, int side)
    {
        if (trackData.Length != SectorsPerTrack * BytesPerSector)
            throw new ArgumentException(
                $"Track data must be {SectorsPerTrack * BytesPerSector} bytes, got {trackData.Length}.",
                nameof(trackData));

        int trackNumber = cylinder * 2 + side;

        var words = new List<ushort>(RawTrackBytes / 2);

        // Leading gap: ~60 words of $AAAA
        for (int i = 0; i < 60; i++)
            words.Add(GapWord);

        for (int sec = 0; sec < SectorsPerTrack; sec++)
        {
            var sectorData = trackData.Slice(sec * BytesPerSector, BytesPerSector).ToArray();
            int sectorsToGap = SectorsPerTrack - sec;

            // Sync words (×2 per sector)
            words.Add(SyncWord);
            words.Add(SyncWord);

            // AmigaDOS sector: odd/even MFM encoding
            EncodeSector(words, sectorData, trackNumber, sec, sectorsToGap);

            // Inter-sector gap: 2 words
            words.Add(GapWord);
            words.Add(GapWord);
        }

        // Pad to raw track length
        while (words.Count * 2 < RawTrackBytes)
            words.Add(GapWord);

        // Convert word list to bytes (big-endian)
        var output = new byte[words.Count * 2];
        for (int i = 0; i < words.Count; i++)
        {
            output[i * 2] = (byte)(words[i] >> 8);
            output[i * 2 + 1] = (byte)(words[i] & 0xFF);
        }

        return output;
    }

    /// <summary>
    /// Convenience overload: build from an <see cref="AdfDisk"/> track.
    /// </summary>
    public static byte[] BuildFromAdf(AdfDisk disk, int cylinder, int side)
    {
        return Build(disk.ReadTrack(cylinder, side), cylinder, side);
    }

    // ------------------------------------------------------------------ Encoding

    private static void EncodeSector(List<ushort> words,
        byte[] sectorData, int track, int sector, int sectorsToGap)
    {
        // --- Header long: format(FF) | track | sector | sectorsToGap ---
        uint headerLong = (uint)(0xFF000000 |
                                 ((uint)(byte)track << 16) |
                                 ((uint)(byte)sector << 8) |
                                 (uint)(byte)sectorsToGap);

        // Encode header using odd/even MFM split
        uint headerOdd = (headerLong >> 1) & 0x55555555u;
        uint headerEven = headerLong & 0x55555555u;
        words.Add(AddClockBits((ushort)(headerOdd >> 16)));
        words.Add(AddClockBits((ushort)(headerOdd & 0xFFFF)));
        words.Add(AddClockBits((ushort)(headerEven >> 16)));
        words.Add(AddClockBits((ushort)(headerEven & 0xFFFF)));

        // --- Header OS info (8 words of zero) ---
        for (int i = 0; i < 8; i++)
            words.Add(AddClockBits(0));

        // --- Header checksum ---
        uint hChecksum = headerOdd ^ headerEven;
        // XOR with OS info (all zero, so no change)
        uint hcOdd = (hChecksum >> 1) & 0x55555555u;
        uint hcEven = hChecksum & 0x55555555u;
        words.Add(AddClockBits((ushort)(hcOdd >> 16)));
        words.Add(AddClockBits((ushort)(hcOdd & 0xFFFF)));
        words.Add(AddClockBits((ushort)(hcEven >> 16)));
        words.Add(AddClockBits((ushort)(hcEven & 0xFFFF)));

        // --- Data: odd longs then even longs (128 longs = 512 bytes) ---
        int wordCountBefore = words.Count;
        uint dataChecksum = 0;

        for (int i = 0; i < BytesPerSector; i += 4)
        {
            uint d = (uint)(sectorData[i] << 24 | sectorData[i + 1] << 16 |
                            sectorData[i + 2] << 8 | sectorData[i + 3]);
            uint odd = (d >> 1) & 0x55555555u;
            uint even = d & 0x55555555u;
            dataChecksum ^= odd ^ even;

            words.Add(AddClockBits((ushort)(odd >> 16)));
            words.Add(AddClockBits((ushort)(odd & 0xFFFF)));
            words.Add(AddClockBits((ushort)(even >> 16)));
            words.Add(AddClockBits((ushort)(even & 0xFFFF)));
        }
        _ = wordCountBefore; // suppress unused warning

        // --- Data checksum ---
        uint dcOdd = (dataChecksum >> 1) & 0x55555555u;
        uint dcEven = dataChecksum & 0x55555555u;
        // Insert data checksum before data — reorder: checksum comes before data words
        // Per AmigaDOS layout: header, header_checksum, data_checksum, data
        // We already appended data words, so insert checksum words at the right position.
        // Simpler: rebuild with correct order.
        // NOTE: We already wrote data words above; remove them and reinsert in correct order.
        int dataWordCount = (BytesPerSector / 4) * 4; // 512 bytes = 128 longs = 512 words
        words.RemoveRange(words.Count - dataWordCount, dataWordCount);

        // Data checksum (4 words)
        words.Add(AddClockBits((ushort)(dcOdd >> 16)));
        words.Add(AddClockBits((ushort)(dcOdd & 0xFFFF)));
        words.Add(AddClockBits((ushort)(dcEven >> 16)));
        words.Add(AddClockBits((ushort)(dcEven & 0xFFFF)));

        // Data words (512 words)
        for (int i = 0; i < BytesPerSector; i += 4)
        {
            uint d = (uint)(sectorData[i] << 24 | sectorData[i + 1] << 16 |
                            sectorData[i + 2] << 8 | sectorData[i + 3]);
            uint odd = (d >> 1) & 0x55555555u;
            uint even = d & 0x55555555u;

            words.Add(AddClockBits((ushort)(odd >> 16)));
            words.Add(AddClockBits((ushort)(odd & 0xFFFF)));
            words.Add(AddClockBits((ushort)(even >> 16)));
            words.Add(AddClockBits((ushort)(even & 0xFFFF)));
        }
    }

    /// <summary>
    /// Insert MFM clock bits for a data word containing only data bits (no clock bits set).
    /// Clock bit is set between two consecutive zero data bits.
    /// </summary>
    private static ushort AddClockBits(ushort dataBits)
    {
        ushort result = 0;
        bool prevDataBit = false; // Assume preceding data bit is 0

        for (int b = 15; b >= 0; b--)
        {
            bool dataBit = (dataBits & (1 << b)) != 0;
            bool clockBit = !prevDataBit && !dataBit;

            if (dataBit) result |= (ushort)(1 << b);
            if (clockBit) result |= (ushort)(1 << (b + 1 <= 15 ? b + 1 : 15));

            prevDataBit = dataBit;
        }

        return result;
    }
}
