using System.Collections.Generic;
using Fusion;

public struct PlayerCardZoneData
{
    public PlayerRef Owner;
    public int HP;
    public List<string> Hand;
    public int DeckCount;
    public int DiscardCount;
    public int DrawPhaseNewCards;
    public bool IsDrawPhaseConfirmed;
}
