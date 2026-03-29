namespace AmigaA500.Core.Floppy;

/// <summary>
/// OFS (Old File System) reader — parses AmigaDOS disk structures.
/// Reads root block, directory entries, and file data from ADF images.
/// </summary>
public sealed class OfsFileSystem
{
    private readonly AdfDisk _disk;

    // Constants
    public const int RootBlockSector = 880; // Middle of disk (track 40)
    public const int BlockSize = 512;
    public const int HashTableSize = 72;

    public OfsFileSystem(AdfDisk disk)
    {
        _disk = disk;
    }

    /// <summary>
    /// Read a 512-byte block from the disk by sector number.
    /// </summary>
    public byte[] ReadBlock(int sector)
    {
        int track = sector / AdfDisk.SectorsPerTrack;
        int cylinder = track / 2;
        int side = track % 2;
        int sectorInTrack = sector % AdfDisk.SectorsPerTrack;
        return _disk.ReadSector(cylinder, side, sectorInTrack).ToArray();
    }

    /// <summary>
    /// Read the root block and extract volume name.
    /// </summary>
    public string GetVolumeName()
    {
        var block = ReadBlock(RootBlockSector);
        int nameLen = block[432];
        if (nameLen > 30) nameLen = 30;
        var name = new char[nameLen];
        for (int i = 0; i < nameLen; i++)
            name[i] = (char)block[433 + i];
        return new string(name);
    }

    /// <summary>
    /// Get the root block type to verify it's valid.
    /// </summary>
    public int GetRootBlockType()
    {
        var block = ReadBlock(RootBlockSector);
        return ReadLong(block, 0); // Should be 2 (T_HEADER)
    }

    /// <summary>
    /// List entries in the root directory.
    /// </summary>
    public List<DirectoryEntry> ListRootDirectory()
    {
        var block = ReadBlock(RootBlockSector);
        var entries = new List<DirectoryEntry>();

        // Hash table starts at offset 24, 72 entries
        for (int i = 0; i < HashTableSize; i++)
        {
            int sector = ReadLong(block, 24 + i * 4);
            if (sector == 0) continue;

            // Follow the hash chain
            while (sector != 0 && sector < 1760)
            {
                var entryBlock = ReadBlock(sector);
                int type = ReadLong(entryBlock, 0);
                int secType = ReadLong(entryBlock, 508);

                int entryNameLen = entryBlock[432];
                if (entryNameLen > 30) entryNameLen = 30;
                var entryName = new char[entryNameLen];
                for (int j = 0; j < entryNameLen; j++)
                    entryName[j] = (char)entryBlock[433 + j];

                entries.Add(new DirectoryEntry
                {
                    Name = new string(entryName),
                    Sector = sector,
                    IsDirectory = secType == 2, // ST_USERDIR
                    IsFile = secType == -3,      // ST_FILE
                    Size = secType == -3 ? ReadLong(entryBlock, 324) : 0
                });

                // Next in hash chain
                sector = ReadLong(entryBlock, 496);
            }
        }

        return entries;
    }

    /// <summary>
    /// Read file data from a file header block.
    /// </summary>
    public byte[] ReadFile(int headerSector)
    {
        var header = ReadBlock(headerSector);
        int fileSize = ReadLong(header, 324);
        int dataBlocks = ReadLong(header, 8);
        var data = new byte[fileSize];
        int offset = 0;

        // Data block list starts at offset 308, going backwards
        for (int i = 0; i < Math.Min(dataBlocks, 72); i++)
        {
            int dataSector = ReadLong(header, 308 - i * 4);
            if (dataSector == 0) break;

            var dataBlock = ReadBlock(dataSector);
            // OFS data block: first 24 bytes are header, rest is data
            int dataLen = Math.Min(488, fileSize - offset); // 512 - 24 = 488 data bytes
            Array.Copy(dataBlock, 24, data, offset, dataLen);
            offset += dataLen;
        }

        return data;
    }

    private static int ReadLong(byte[] block, int offset)
    {
        return (block[offset] << 24) | (block[offset + 1] << 16) |
               (block[offset + 2] << 8) | block[offset + 3];
    }
}

public class DirectoryEntry
{
    public string Name = "";
    public int Sector;
    public bool IsDirectory;
    public bool IsFile;
    public int Size;

    public override string ToString() =>
        IsDirectory ? $"[{Name}]/" : $"{Name} ({Size} bytes)";
}
