namespace AmigaA500.Core.Floppy;

/// <summary>
/// FFS (Fast File System) reader — differs from OFS in that data blocks
/// don't have headers, giving 512 bytes of data per block instead of 488.
/// </summary>
public sealed class FfsFileSystem
{
    private readonly AdfDisk _disk;

    public FfsFileSystem(AdfDisk disk)
    {
        _disk = disk;
    }

    public byte[] ReadBlock(int sector)
    {
        int track = sector / AdfDisk.SectorsPerTrack;
        int cylinder = track / 2;
        int side = track % 2;
        int sectorInTrack = sector % AdfDisk.SectorsPerTrack;
        return _disk.ReadSector(cylinder, side, sectorInTrack).ToArray();
    }

    public string GetVolumeName()
    {
        var block = ReadBlock(OfsFileSystem.RootBlockSector);
        int nameLen = Math.Min(block[432], (byte)30);
        return new string(block.AsSpan(433, nameLen).ToArray().Select(b => (char)b).ToArray());
    }

    /// <summary>
    /// Read file data. FFS data blocks are pure data (no 24-byte header).
    /// </summary>
    public byte[] ReadFile(int headerSector)
    {
        var header = ReadBlock(headerSector);
        int fileSize = ReadLong(header, 324);
        int dataBlockCount = ReadLong(header, 8);
        var data = new byte[fileSize];
        int offset = 0;

        // Data block pointers at offsets 308 down to 24 (72 entries max)
        for (int i = 0; i < Math.Min(dataBlockCount, 72); i++)
        {
            int dataSector = ReadLong(header, 308 - i * 4);
            if (dataSector == 0) break;

            var dataBlock = ReadBlock(dataSector);
            // FFS: entire 512 bytes is data (no header)
            int copyLen = Math.Min(512, fileSize - offset);
            Array.Copy(dataBlock, 0, data, offset, copyLen);
            offset += copyLen;
        }

        // Handle extension blocks for files > 72 data blocks
        int extBlock = ReadLong(header, 496);
        while (extBlock != 0 && offset < fileSize)
        {
            var ext = ReadBlock(extBlock);
            int extCount = ReadLong(ext, 8);
            for (int i = 0; i < Math.Min(extCount, 72); i++)
            {
                int dataSector = ReadLong(ext, 308 - i * 4);
                if (dataSector == 0) break;

                var dataBlock = ReadBlock(dataSector);
                int copyLen = Math.Min(512, fileSize - offset);
                Array.Copy(dataBlock, 0, data, offset, copyLen);
                offset += copyLen;
            }
            extBlock = ReadLong(ext, 496);
        }

        return data;
    }

    private static int ReadLong(byte[] block, int offset)
    {
        return (block[offset] << 24) | (block[offset + 1] << 16) |
               (block[offset + 2] << 8) | block[offset + 3];
    }
}
