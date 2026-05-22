using System.Collections.Generic;
using Fusion;

// Private per-player card zone data — replicated only to Owner via AoI
public struct PlayerCardZonePrivateData
{
    public PlayerRef Owner;
    public List<string> Hand; // capacity 6
}
