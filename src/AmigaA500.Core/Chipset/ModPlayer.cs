namespace AmigaA500.Core.Chipset;

/// <summary>
/// Basic MOD/ProTracker module player — interprets the standard 4-channel Amiga module format.
/// Used for testing audio output with real music data.
/// </summary>
public sealed class ModPlayer
{
    public const int NumChannels = 4;
    public const int RowsPerPattern = 64;

    private readonly byte[] _moduleData;
    private int _currentPosition;
    private int _currentRow;
    private int _speed = 6;
    private int _tickCounter;
    private int _bpm = 125;

    public string Title { get; }
    public int NumPositions { get; }
    public int NumPatterns { get; }
    public bool IsPlaying { get; set; }

    public ModPlayer(byte[] moduleData)
    {
        _moduleData = moduleData;

        // Parse MOD header
        if (moduleData.Length < 1084)
            throw new ArgumentException("Not a valid MOD file");

        // Title: bytes 0-19
        Title = System.Text.Encoding.ASCII.GetString(moduleData, 0, 20).TrimEnd('\0');

        // Song length (number of positions): byte 950
        NumPositions = moduleData[950];

        // Count patterns (highest pattern number in position table + 1)
        int maxPattern = 0;
        for (int i = 0; i < 128; i++)
        {
            int pat = moduleData[952 + i];
            if (pat > maxPattern) maxPattern = pat;
        }
        NumPatterns = maxPattern + 1;
    }

    /// <summary>
    /// Check if data looks like a valid MOD file.
    /// </summary>
    public static bool IsModFile(ReadOnlySpan<byte> data)
    {
        if (data.Length < 1084) return false;

        // Check for format tag at offset 1080
        string tag = System.Text.Encoding.ASCII.GetString(data.Slice(1080, 4));
        return tag is "M.K." or "M!K!" or "FLT4" or "FLT8" or "4CHN" or "6CHN" or "8CHN";
    }

    /// <summary>
    /// Get the pattern number for a given position in the song.
    /// </summary>
    public int GetPatternAtPosition(int position)
    {
        if (position < 0 || position >= 128) return 0;
        return _moduleData[952 + position];
    }

    /// <summary>
    /// Advance one tick. Returns true when a new row starts.
    /// </summary>
    public bool Tick()
    {
        if (!IsPlaying) return false;

        _tickCounter++;
        if (_tickCounter >= _speed)
        {
            _tickCounter = 0;
            _currentRow++;
            if (_currentRow >= RowsPerPattern)
            {
                _currentRow = 0;
                _currentPosition++;
                if (_currentPosition >= NumPositions)
                {
                    _currentPosition = 0; // Loop
                }
            }
            return true;
        }
        return false;
    }

    public int CurrentPosition => _currentPosition;
    public int CurrentRow => _currentRow;
    public int Speed => _speed;
    public int Bpm => _bpm;

    public void SetSpeed(int speed) { if (speed > 0) _speed = speed; }
    public void SetBpm(int bpm) { if (bpm > 0) _bpm = bpm; }
    public void Reset() { _currentPosition = 0; _currentRow = 0; _tickCounter = 0; }
}
