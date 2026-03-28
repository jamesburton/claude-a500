namespace AmigaA500.Core.Chipset;

/// <summary>
/// Amiga hardware sprite engine — 8 sprites, 16 pixels wide, 3 colors each.
/// </summary>
public sealed class SpriteEngine
{
    public const int SpriteCount = 8;

    private readonly SpriteState[] _sprites = new SpriteState[SpriteCount];

    public SpriteEngine()
    {
        for (int i = 0; i < SpriteCount; i++)
            _sprites[i] = new SpriteState();
    }

    public void SetPosition(int sprite, ushort pos, ushort ctl)
    {
        var s = _sprites[sprite];
        s.HStart = ((pos & 0xFF) << 1) | (ctl & 1);
        s.VStart = ((pos >> 8) & 0xFF) | ((ctl & 0x04) << 6);
        s.VStop = ((ctl >> 8) & 0xFF) | ((ctl & 0x02) << 7);
        s.Armed = true;
    }

    public void SetData(int sprite, ushort dataA, ushort dataB)
    {
        _sprites[sprite].DataA = dataA;
        _sprites[sprite].DataB = dataB;
    }

    /// <summary>
    /// Get sprite pixel color at given position, or null if transparent.
    /// Returns color register index (16-19 for sprite pair 0-1, etc.)
    /// </summary>
    public int? GetSpritePixel(int hpos, int vpos)
    {
        // Check sprites in priority order (lower index = higher priority)
        for (int i = 0; i < SpriteCount; i++)
        {
            var s = _sprites[i];
            if (!s.Armed) continue;
            if (vpos < s.VStart || vpos >= s.VStop) continue;

            int pixel = hpos - s.HStart;
            if (pixel < 0 || pixel >= 16) continue;

            int bit = 15 - pixel;
            int color = ((s.DataA >> bit) & 1) | (((s.DataB >> bit) & 1) << 1);
            if (color == 0) continue; // Transparent

            // Color register: 16 + (pair * 4) + color
            return 16 + (i / 2) * 4 + color;
        }
        return null;
    }

    public bool IsAttached(int spriteA, int spriteB)
    {
        // Sprite pairs can be attached for 15-color sprites
        return spriteA + 1 == spriteB && (spriteA & 1) == 0;
    }

    private class SpriteState
    {
        public int HStart, VStart, VStop;
        public ushort DataA, DataB;
        public bool Armed;
    }
}
