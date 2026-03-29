namespace AmigaA500.Core.Chipset;

/// <summary>
/// Handles interlaced display modes — alternates between even and odd fields.
/// In interlace mode, PAL gives 512 lines (2×256) and NTSC gives 400 lines (2×200).
/// </summary>
public sealed class InterlaceHandler
{
    private bool _oddField;
    private readonly uint[] _evenField;
    private readonly uint[] _oddFieldBuffer;

    public int Width { get; }
    public int FieldHeight { get; }
    public int FrameHeight => FieldHeight * 2;
    public bool IsOddField => _oddField;

    public InterlaceHandler(int width = 320, int fieldHeight = 256)
    {
        Width = width;
        FieldHeight = fieldHeight;
        _evenField = new uint[width * fieldHeight];
        _oddFieldBuffer = new uint[width * fieldHeight];
    }

    /// <summary>
    /// Store the current field's framebuffer data.
    /// </summary>
    public void StoreField(ReadOnlySpan<uint> fieldData)
    {
        var target = _oddField ? _oddFieldBuffer.AsSpan() : _evenField.AsSpan();
        fieldData[..(Width * FieldHeight)].CopyTo(target);
        _oddField = !_oddField;
    }

    /// <summary>
    /// Weave even and odd fields into a full-height interlaced frame.
    /// </summary>
    public void WeaveFields(Span<uint> output)
    {
        for (int y = 0; y < FieldHeight; y++)
        {
            // Even field → even lines (0, 2, 4, ...)
            _evenField.AsSpan(y * Width, Width).CopyTo(output[(y * 2 * Width)..]);
            // Odd field → odd lines (1, 3, 5, ...)
            _oddFieldBuffer.AsSpan(y * Width, Width).CopyTo(output[((y * 2 + 1) * Width)..]);
        }
    }

    /// <summary>
    /// Deinterlace using bob (field doubling) — each field shown as full frame.
    /// Faster but lower quality than weaving.
    /// </summary>
    public void BobDeinterlace(ReadOnlySpan<uint> field, Span<uint> output)
    {
        for (int y = 0; y < FieldHeight; y++)
        {
            var srcLine = field.Slice(y * Width, Width);
            srcLine.CopyTo(output[(y * 2 * Width)..]);
            srcLine.CopyTo(output[((y * 2 + 1) * Width)..]);
        }
    }

    public void Reset()
    {
        _oddField = false;
        Array.Clear(_evenField);
        Array.Clear(_oddFieldBuffer);
    }
}
