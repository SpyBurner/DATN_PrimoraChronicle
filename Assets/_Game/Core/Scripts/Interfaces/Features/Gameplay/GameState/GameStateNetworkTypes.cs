public struct GameStateStateData
{
    public int CurrentTurn;
    public string CurrentPhase;
    public int MatchTimer;
}

public interface IGameStateNetworkBridge
{
    void SendStartMatchRpc();
    void SendEndTurnRpc();
    void SendSetPhaseRpc(string phase);
}
