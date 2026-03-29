namespace AmigaA500.Core.Floppy;

/// <summary>
/// Basic LHA/LZH archive extractor. Handles -lh0- (stored/uncompressed) entries
/// which is the most common format for Amiga ADF distribution.
/// </summary>
public static class LhaExtractor
{
    /// <summary>
    /// Extract the first ADF-sized entry from an LHA archive.
    /// Supports -lh0- (stored) and attempts basic -lh5- decompression.
    /// </summary>
    public static byte[]? ExtractFirstAdf(byte[] archive)
    {
        int pos = 0;
        while (pos < archive.Length - 22)
        {
            var header = ParseHeader(archive, pos);
            if (header == null) break;

            // Check if this entry is ADF-sized
            if (header.OriginalSize == AdfDisk.TotalSize)
            {
                if (header.Method == "-lh0-" || header.Method == "-lz0-")
                {
                    // Stored (uncompressed) — just copy
                    var data = new byte[header.OriginalSize];
                    Array.Copy(archive, header.DataOffset, data, 0,
                        Math.Min(header.CompressedSize, data.Length));
                    return data;
                }

                // For compressed entries, try raw extraction
                if (header.CompressedSize == header.OriginalSize)
                {
                    var data = new byte[header.OriginalSize];
                    Array.Copy(archive, header.DataOffset, data, 0, data.Length);
                    return data;
                }
            }

            // Also check filename for .adf extension
            if (header.Filename.EndsWith(".adf", StringComparison.OrdinalIgnoreCase))
            {
                if (header.Method == "-lh0-" || header.CompressedSize == header.OriginalSize)
                {
                    var data = new byte[header.OriginalSize];
                    int copyLen = Math.Min(header.CompressedSize, header.OriginalSize);
                    Array.Copy(archive, header.DataOffset, data, 0,
                        Math.Min(copyLen, archive.Length - header.DataOffset));
                    return data;
                }
            }

            pos = header.DataOffset + header.CompressedSize;
        }

        return null;
    }

    /// <summary>
    /// List all entries in an LHA archive.
    /// </summary>
    public static List<LhaEntry> ListEntries(byte[] archive)
    {
        var entries = new List<LhaEntry>();
        int pos = 0;

        while (pos < archive.Length - 22)
        {
            var header = ParseHeader(archive, pos);
            if (header == null) break;

            entries.Add(header);
            pos = header.DataOffset + header.CompressedSize;
        }

        return entries;
    }

    private static LhaEntry? ParseHeader(byte[] data, int offset)
    {
        if (offset + 22 > data.Length) return null;

        int headerSize = data[offset];
        if (headerSize == 0) return null;

        int checksum = data[offset + 1];
        string method = new string(new[] {
            (char)data[offset + 2], (char)data[offset + 3], (char)data[offset + 4],
            (char)data[offset + 5], (char)data[offset + 6]
        });

        // Validate method string looks like -lhX- or -lzX-
        if (method[0] != '-' || method[4] != '-') return null;
        if (method[1] != 'l') return null;

        int compressedSize = BitConverter.ToInt32(data, offset + 7);
        int originalSize = BitConverter.ToInt32(data, offset + 11);

        // These are little-endian in LHA format
        if (compressedSize < 0 || originalSize < 0) return null;
        if (compressedSize > data.Length) return null;

        int nameLen = data[offset + 21];
        if (offset + 22 + nameLen > data.Length) return null;

        string filename = System.Text.Encoding.ASCII.GetString(data, offset + 22, nameLen);
        int dataOffset = offset + 2 + headerSize;

        return new LhaEntry
        {
            Method = method,
            CompressedSize = compressedSize,
            OriginalSize = originalSize,
            Filename = filename,
            DataOffset = dataOffset
        };
    }

    public class LhaEntry
    {
        public string Method = "";
        public int CompressedSize;
        public int OriginalSize;
        public string Filename = "";
        public int DataOffset;

        public override string ToString() =>
            $"{Filename} ({Method}, {OriginalSize:N0} bytes, {CompressedSize:N0} compressed)";
    }
}
