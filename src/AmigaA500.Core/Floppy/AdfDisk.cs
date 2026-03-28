namespace AmigaA500.Core.Floppy;

/// <summary>
/// ADF (Amiga Disk File) reader — 880 KB raw sector dump format.
/// </summary>
public sealed class AdfDisk
{
    public const int TracksPerSide = 80;
    public const int Sides = 2;
    public const int SectorsPerTrack = 11;
    public const int BytesPerSector = 512;
    public const int BytesPerTrack = SectorsPerTrack * BytesPerSector; // 5632
    public const int TotalSize = TracksPerSide * Sides * BytesPerTrack; // 901120

    private readonly byte[] _data;

    public AdfDisk(byte[] data)
    {
        if (data.Length != TotalSize)
            throw new ArgumentException($"ADF must be {TotalSize} bytes, got {data.Length}");
        _data = data;
    }

    public static AdfDisk Load(string path) => new(File.ReadAllBytes(path));

    /// <summary>
    /// Read a complete track (11 sectors = 5632 bytes).
    /// Track numbering: track = cylinder * 2 + side.
    /// </summary>
    public ReadOnlySpan<byte> ReadTrack(int cylinder, int side)
    {
        int trackNum = cylinder * 2 + side;
        int offset = trackNum * BytesPerTrack;
        return _data.AsSpan(offset, BytesPerTrack);
    }

    /// <summary>
    /// Read a single sector (512 bytes).
    /// </summary>
    public ReadOnlySpan<byte> ReadSector(int cylinder, int side, int sector)
    {
        int trackNum = cylinder * 2 + side;
        int offset = trackNum * BytesPerTrack + sector * BytesPerSector;
        return _data.AsSpan(offset, BytesPerSector);
    }

    /// <summary>
    /// Read the bootblock (sectors 0-1 of track 0, side 0 = first 1024 bytes).
    /// </summary>
    public ReadOnlySpan<byte> ReadBootblock()
    {
        return _data.AsSpan(0, 1024);
    }

    /// <summary>
    /// Validate the bootblock: check for "DOS" magic number and checksum.
    /// </summary>
    public bool IsBootable()
    {
        if (_data.Length < 1024) return false;

        // Check magic: "DOS\0" or "DOS\1" (FFS) at offset 0
        if (_data[0] != 'D' || _data[1] != 'O' || _data[2] != 'S')
            return false;

        // Validate checksum (sum of all longs in bootblock should be 0)
        uint checksum = 0;
        for (int i = 0; i < 1024; i += 4)
        {
            uint word = (uint)(_data[i] << 24 | _data[i + 1] << 16 | _data[i + 2] << 8 | _data[i + 3]);
            uint prev = checksum;
            checksum += word;
            if (checksum < prev) checksum++; // Carry
        }

        return checksum == 0xFFFFFFFF;
    }

    /// <summary>
    /// Get the disk type from the bootblock.
    /// </summary>
    public DiskType GetDiskType()
    {
        if (_data.Length < 4) return DiskType.Unknown;
        return (_data[0], _data[1], _data[2], _data[3]) switch
        {
            ((byte)'D', (byte)'O', (byte)'S', 0) => DiskType.OFS,     // Old File System
            ((byte)'D', (byte)'O', (byte)'S', 1) => DiskType.FFS,     // Fast File System
            ((byte)'D', (byte)'O', (byte)'S', 2) => DiskType.OFS_Intl, // OFS International
            ((byte)'D', (byte)'O', (byte)'S', 3) => DiskType.FFS_Intl, // FFS International
            _ => DiskType.Unknown
        };
    }
}

public enum DiskType
{
    Unknown,
    OFS,        // Original File System
    FFS,        // Fast File System
    OFS_Intl,   // OFS International mode
    FFS_Intl    // FFS International mode
}
