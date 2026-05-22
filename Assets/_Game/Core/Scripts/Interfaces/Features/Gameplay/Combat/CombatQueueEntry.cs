using Fusion;

public struct CombatQueueEntry
{
    public NetworkId UnitId;
    public string CardId; // unit card id — used by TurnOrderPanel to fetch the card image
}
