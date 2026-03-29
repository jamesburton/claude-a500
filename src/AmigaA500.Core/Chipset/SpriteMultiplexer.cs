namespace AmigaA500.Core.Chipset;

/// <summary>
/// Sprite multiplexer — allows reusing hardware sprites multiple times per frame
/// by reprogramming sprite position and data via the Copper.
/// Tracks sprite reuse for accurate rendering.
/// </summary>
public sealed class SpriteMultiplexer
{
    private readonly SpriteEngine _sprites;
    private readonly int[][] _spritePositions; // Track position changes per line

    public SpriteMultiplexer(SpriteEngine sprites)
    {
        _sprites = sprites;
        _spritePositions = new int[SpriteEngine.SpriteCount][];
        for (int i = 0; i < SpriteEngine.SpriteCount; i++)
            _spritePositions[i] = new int[312]; // PAL max lines
    }

    /// <summary>
    /// Record that a sprite was repositioned at a given line (via Copper MOVE).
    /// </summary>
    public void RecordRepositioning(int sprite, int line, int newHPos)
    {
        if (sprite >= 0 && sprite < SpriteEngine.SpriteCount && line >= 0 && line < 312)
            _spritePositions[sprite][line] = newHPos;
    }

    /// <summary>
    /// Get the effective sprite position for a given line, accounting for multiplexing.
    /// </summary>
    public int GetEffectiveHPos(int sprite, int line)
    {
        if (sprite < 0 || sprite >= SpriteEngine.SpriteCount || line < 0 || line >= 312)
            return 0;

        // Walk backward from the current line to find the last position set
        for (int l = line; l >= 0; l--)
        {
            if (_spritePositions[sprite][l] != 0)
                return _spritePositions[sprite][l];
        }
        return 0;
    }

    /// <summary>
    /// Reset all multiplexing state at start of frame.
    /// </summary>
    public void ResetFrame()
    {
        for (int i = 0; i < SpriteEngine.SpriteCount; i++)
            Array.Clear(_spritePositions[i]);
    }
}
