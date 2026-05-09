public struct MatchResultStateData
{
    public bool IsVictory;
    public int GoldEarned;
    public int RankProgress;
}

public interface IMatchResultNetworkBridge
{
    void SendShowResultRpc(bool victory, int gold, int rank);
    void SendBackToLobbyRpc();
}
