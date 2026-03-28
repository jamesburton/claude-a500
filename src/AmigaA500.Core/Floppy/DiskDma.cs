namespace AmigaA500.Core.Floppy;

/// <summary>
/// Disk DMA controller — transfers data between floppy and chip RAM.
/// </summary>
public sealed class DiskDma
{
    private readonly Func<uint, ushort> _dmaRead;
    private readonly Action<uint, ushort> _dmaWrite;
    private readonly Action? _onComplete;

    public uint DskPt;
    public ushort DskLen;
    public ushort DskSync = 0x4489;

    private bool _dmaActive;
    private bool _syncFound;
    private int _wordsRemaining;
    private bool _writing;
    private ushort _prevLen;

    public bool Active => _dmaActive;

    public DiskDma(Func<uint, ushort> dmaRead, Action<uint, ushort> dmaWrite, Action? onComplete = null)
    {
        _dmaRead = dmaRead;
        _dmaWrite = dmaWrite;
        _onComplete = onComplete;
    }

    /// <summary>
    /// Write to DSKLEN register. DMA starts on second consecutive write with bit 15 set.
    /// </summary>
    public void WriteDskLen(ushort value)
    {
        if ((value & 0x8000) != 0 && (_prevLen & 0x8000) != 0)
        {
            // Double write with DMA enable — start transfer
            _wordsRemaining = value & 0x3FFF;
            _writing = (value & 0x4000) != 0;
            _dmaActive = true;
            _syncFound = false;
        }
        else if ((value & 0x8000) == 0)
        {
            // Disable DMA
            _dmaActive = false;
        }
        _prevLen = value;
        DskLen = value;
    }

    /// <summary>
    /// Process one DMA cycle. Call during disk DMA slots.
    /// </summary>
    public void ExecuteCycle(FloppyDrive drive)
    {
        if (!_dmaActive || _wordsRemaining <= 0) return;

        if (_writing)
        {
            // Write: read from chip RAM, write to disk (not implemented yet)
            return;
        }

        // Read: get word from drive
        ushort word = drive.ReadNextWord();

        // Wait for sync word before transferring data
        if (!_syncFound)
        {
            if (word == DskSync)
            {
                _syncFound = true;
            }
            return;
        }

        // Transfer word to chip RAM
        _dmaWrite(DskPt, word);
        DskPt += 2;
        _wordsRemaining--;

        if (_wordsRemaining == 0)
        {
            _dmaActive = false;
            _onComplete?.Invoke();
        }
    }

    public void Reset()
    {
        _dmaActive = false;
        _syncFound = false;
        _wordsRemaining = 0;
        _prevLen = 0;
    }
}
