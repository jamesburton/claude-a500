namespace AmigaA500.Core.Floppy;

/// <summary>
/// Amiga floppy drive emulation — manages head position, motor, and data reading.
/// </summary>
public sealed class FloppyDrive
{
    private AdfDisk? _disk;
    private byte[]? _trackBuffer;
    private int _trackBufferOffset;

    public int CurrentCylinder { get; private set; }
    public int CurrentSide { get; private set; }
    public bool MotorOn { get; set; }
    public bool Selected { get; set; }

    public bool DiskInserted => _disk != null;
    public bool Ready => MotorOn && DiskInserted;
    public bool AtTrackZero => CurrentCylinder == 0;
    public bool WriteProtected => true; // Read-only for now

    public void InsertDisk(AdfDisk disk)
    {
        _disk = disk;
        LoadTrackBuffer();
    }

    public void EjectDisk()
    {
        _disk = null;
        _trackBuffer = null;
    }

    public void StepHead(bool towardCenter)
    {
        if (towardCenter && CurrentCylinder < AdfDisk.TracksPerSide - 1)
            CurrentCylinder++;
        else if (!towardCenter && CurrentCylinder > 0)
            CurrentCylinder--;

        LoadTrackBuffer();
    }

    public void SelectSide(int side)
    {
        CurrentSide = side & 1;
        LoadTrackBuffer();
    }

    public ushort ReadNextWord()
    {
        if (_trackBuffer == null || !Ready)
            return 0xAAAA; // No data — return MFM clock pattern

        ushort word = (ushort)(_trackBuffer[_trackBufferOffset] << 8 |
                               _trackBuffer[_trackBufferOffset + 1]);
        _trackBufferOffset = (_trackBufferOffset + 2) % _trackBuffer.Length;
        return word;
    }

    public int TrackLength => _trackBuffer?.Length ?? 0;
    public int TrackPosition => _trackBufferOffset;

    private void LoadTrackBuffer()
    {
        if (_disk == null) { _trackBuffer = null; return; }

        var track = _disk.ReadTrack(CurrentCylinder, CurrentSide);
        _trackBuffer = track.ToArray();
        _trackBufferOffset = 0;
    }
}
