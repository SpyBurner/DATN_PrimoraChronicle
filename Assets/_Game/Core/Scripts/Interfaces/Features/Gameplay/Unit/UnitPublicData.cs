using System.Collections.Generic;
using Fusion;

public struct UnitPublicData
{
    public NetworkId UnitId;
    public PlayerRef Owner;
    public HexCoord Position;
    public int CurrentHP;
    public int MaxHP;
    public float Speed;
    public int DeathAnchor;
    public bool IsPersistent;
    public int GrowthStacks;
    public List<StatusSlot> StatusEffects;
}
