namespace AmigaA500.Core.Chipset;

/// <summary>
/// Hardware collision detection — sprite-sprite and sprite-playfield collisions.
/// </summary>
public sealed class CollisionDetector
{
    public ushort CLXDAT;
    public ushort CLXCON;

    public void Reset() => CLXDAT = 0;

    public ushort ReadAndClear()
    {
        ushort val = CLXDAT;
        CLXDAT = 0;
        return val;
    }

    /// <summary>
    /// Check collisions for current pixel position.
    /// </summary>
    public void CheckCollisions(bool[] spriteActive, bool pf1Active, bool pf2Active)
    {
        // Sprite-sprite collisions (pairs)
        for (int i = 0; i < 8; i += 2)
        {
            for (int j = i + 2; j < 8; j += 2)
            {
                if ((spriteActive[i] || spriteActive[i + 1]) &&
                    (spriteActive[j] || spriteActive[j + 1]))
                {
                    int bit = GetSpritePairCollisionBit(i / 2, j / 2);
                    if (bit >= 0) CLXDAT |= (ushort)(1 << bit);
                }
            }
        }

        // Sprite-playfield collisions
        for (int i = 0; i < 8; i += 2)
        {
            if (!spriteActive[i] && !spriteActive[i + 1]) continue;

            bool matchPf1 = pf1Active && ((CLXCON & (1 << (6 + i / 2))) != 0 || true);
            bool matchPf2 = pf2Active && ((CLXCON & (1 << (6 + i / 2))) != 0 || true);

            if (matchPf1) CLXDAT |= (ushort)(1 << (1 + i / 2));
            if (matchPf2) CLXDAT |= (ushort)(1 << (5 + i / 2));
        }

        // Playfield 1 - Playfield 2 collision
        if (pf1Active && pf2Active)
            CLXDAT |= 0x0001;
    }

    private static int GetSpritePairCollisionBit(int pair1, int pair2)
    {
        // Bit mapping for sprite pair collisions
        return (pair1, pair2) switch
        {
            (0, 1) => 9,
            (0, 2) => 10,
            (0, 3) => 11,
            (1, 2) => 12,
            (1, 3) => 13,
            (2, 3) => 14,
            _ => -1
        };
    }
}
