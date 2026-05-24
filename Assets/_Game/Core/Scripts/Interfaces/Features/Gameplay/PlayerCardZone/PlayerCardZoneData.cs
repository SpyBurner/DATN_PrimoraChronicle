using System.Collections.Generic;
using Fusion;

// Private per-player card zone data — replicated only to Owner via AoI
public struct PlayerCardZonePrivateData
{
    public PlayerRef Owner;
    public int HP;
    public List<string> Hand;
    public int DeckCount;
    public int DiscardCount;
    public int DrawPhaseNewCards;
    public bool IsDrawPhaseConfirmed;
}
