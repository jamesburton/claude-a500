namespace AmigaA500.Core.Floppy;

/// <summary>
/// Hard drive emulation using HDF (Hard Disk File) images.
/// Supports raw sector-level access and basic RDB (Rigid Disk Block) parsing.
/// </summary>
public sealed class HardDrive
{
    private readonly byte[] _data;
    public const int SectorSize = 512;

    public int TotalSectors => _data.Length / SectorSize;
    public long TotalBytes => _data.Length;
    public int CylindersPerSurface { get; set; } = 1024;
    public int SurfacesPerCylinder { get; set; } = 1;
    public int SectorsPerTrack { get; set; } = 32;

    public HardDrive(byte[] data)
    {
        _data = data;
    }

    public static HardDrive Load(string path) => new(File.ReadAllBytes(path));

    public static HardDrive Create(int sizeInMb)
    {
        return new HardDrive(new byte[sizeInMb * 1024 * 1024]);
    }

    public byte[] ReadSector(int sector)
    {
        int offset = sector * SectorSize;
        if (offset + SectorSize > _data.Length)
            throw new ArgumentOutOfRangeException(nameof(sector));

        var result = new byte[SectorSize];
        Array.Copy(_data, offset, result, 0, SectorSize);
        return result;
    }

    public void WriteSector(int sector, byte[] data)
    {
        if (data.Length != SectorSize)
            throw new ArgumentException($"Sector must be {SectorSize} bytes");

        int offset = sector * SectorSize;
        if (offset + SectorSize > _data.Length)
            throw new ArgumentOutOfRangeException(nameof(sector));

        Array.Copy(data, 0, _data, offset, SectorSize);
    }

    public ushort ReadWord(int sector, int offset)
    {
        int absOffset = sector * SectorSize + offset;
        return (ushort)(_data[absOffset] << 8 | _data[absOffset + 1]);
    }

    public uint ReadLong(int sector, int offset)
    {
        int absOffset = sector * SectorSize + offset;
        return (uint)(_data[absOffset] << 24 | _data[absOffset + 1] << 16 |
                      _data[absOffset + 2] << 8 | _data[absOffset + 3]);
    }

    /// <summary>
    /// Check if the HDF has a Rigid Disk Block (RDB) at sector 0-15.
    /// </summary>
    public bool HasRdb()
    {
        for (int s = 0; s < Math.Min(16, TotalSectors); s++)
        {
            if (ReadLong(s, 0) == 0x5244534B) // "RDSK"
                return true;
        }
        return false;
    }

    /// <summary>
    /// Parse the RDB to get partition info.
    /// </summary>
    public List<HdfPartition> GetPartitions()
    {
        var partitions = new List<HdfPartition>();

        for (int s = 0; s < Math.Min(16, TotalSectors); s++)
        {
            if (ReadLong(s, 0) != 0x5244534B) continue; // "RDSK"

            // RDB found — read partition list
            int partBlock = (int)ReadLong(s, 28); // rdb_PartitionList

            while (partBlock != -1 && partBlock != 0xFFFFFFFF && partBlock < TotalSectors)
            {
                if (ReadLong(partBlock, 0) != 0x50415254) break; // "PART"

                int nameLen = _data[partBlock * SectorSize + 36];
                string name = System.Text.Encoding.ASCII.GetString(
                    _data, partBlock * SectorSize + 37, Math.Min(nameLen, 30));

                partitions.Add(new HdfPartition
                {
                    Name = name,
                    StartSector = (int)ReadLong(partBlock, 164) * SectorsPerTrack * SurfacesPerCylinder,
                    EndSector = (int)(ReadLong(partBlock, 168) + 1) * SectorsPerTrack * SurfacesPerCylinder,
                });

                partBlock = (int)ReadLong(partBlock, 16); // pb_Next
            }
            break;
        }

        return partitions;
    }

    public void SaveToFile(string path)
    {
        File.WriteAllBytes(path, _data);
    }
}

public class HdfPartition
{
    public string Name = "";
    public int StartSector;
    public int EndSector;
    public int SizeInSectors => EndSector - StartSector;

    public override string ToString() => $"{Name}: sectors {StartSector}-{EndSector} ({SizeInSectors * 512 / 1024}K)";
}
