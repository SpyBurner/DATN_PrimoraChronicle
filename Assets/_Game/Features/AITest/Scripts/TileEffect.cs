using System;

public enum TileEffectType
{
    Burning,
    Poison,
    Rooted,
    Frost,
    Healing,
    Corrupted,
    Seeded,
    Entangled,
    AshCloud
}

[Serializable]
public class TileEffect
{
    public TileEffectType Type;
    public int Duration;
    public int OwnerPlayer;
    public int DamagePerTurn;

    public TileEffect(TileEffectType type, int duration, int ownerPlayer, int damagePerTurn = 0)
    {
        Type = type;
        Duration = duration;
        OwnerPlayer = ownerPlayer;
        DamagePerTurn = damagePerTurn;
    }

    public bool IsExpired => Duration <= 0;

    public void Tick()
    {
        if (Duration > 0)
            Duration--;
    }
}
