using Fusion;

// Per-player rewards — replicated only to Owner via AoI after match end
public struct MatchRewardsPrivateData
{
    public PlayerRef Owner;
    public int GoldEarned;
    public int XPEarned;
}
