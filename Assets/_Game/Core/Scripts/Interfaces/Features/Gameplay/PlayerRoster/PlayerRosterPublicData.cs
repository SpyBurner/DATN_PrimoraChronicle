using Fusion;

// Per-player public profile data — always replicated to all clients
public struct PlayerRosterPublicData
{
    public PlayerRef Owner;
    public int HP;
    public string PlayerName;
    public string UserId; // for HTTP avatar fetch per-client
}
