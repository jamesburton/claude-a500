namespace AmigaA500.Core.Chipset;

/// <summary>
/// Amiga built-in serial port emulation.
/// </summary>
public sealed class SerialPort
{
    public ushort SERDAT;    // Transmit data
    public ushort SERDATR;   // Receive data + status
    public ushort SERPER;    // Baud rate period

    private readonly Queue<byte> _txBuffer = new();
    private readonly Queue<byte> _rxBuffer = new();
    private readonly Action<int>? _requestInterrupt;

    public SerialPort(Action<int>? requestInterrupt = null)
    {
        _requestInterrupt = requestInterrupt;
        SERDATR = 0x3000; // TBE + TSRE (transmit buffer empty)
    }

    public void WriteSerdat(ushort value)
    {
        SERDAT = value;
        byte data = (byte)(value & 0xFF);
        _txBuffer.Enqueue(data);
        SERDATR &= 0xCFFF; // Clear TBE and TSRE

        // Transmit immediately (simplified)
        SERDATR |= 0x3000; // Set TBE + TSRE
        _requestInterrupt?.Invoke(0); // TBE interrupt (bit 0)
    }

    public void ReceiveByte(byte data)
    {
        _rxBuffer.Enqueue(data);
        SERDATR = (ushort)((SERDATR & 0xFF00) | data | 0x4000); // Set RBF
        _requestInterrupt?.Invoke(11); // RBF interrupt
    }

    public bool TxAvailable => _txBuffer.Count > 0;
    public byte ReadTx() => _txBuffer.Count > 0 ? _txBuffer.Dequeue() : (byte)0;

    public int BaudRate => SERPER > 0 ? (int)(3546895.0 / (SERPER + 1)) : 0;
}
