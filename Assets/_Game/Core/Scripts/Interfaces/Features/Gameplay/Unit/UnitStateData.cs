using System.Collections.Generic;
using Fusion;

public struct UnitStateData
{
    public string UnitNetworkId;
    public PlayerRef Owner;
    public HexCoord Position;
    public int CurrentHP;
    public int MaxHP;
    public float Speed;
    public int DeathAnchor;
    public int MoveRange;
    public string Faction;
    public bool IsPersistent;
    public int GrowthStacks;
    public List<StatusSlot> StatusEffects;
    public List<SkillSlot> Skills;
}
