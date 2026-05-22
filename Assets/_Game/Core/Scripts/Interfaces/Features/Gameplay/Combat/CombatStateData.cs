using System.Collections.Generic;
using Fusion;

public struct CombatStateData
{
    public NetworkId CurrentActor;
    public bool HasMoved;
    public bool HasActed;
    public List<CombatQueueEntry> ActionQueue;
}
