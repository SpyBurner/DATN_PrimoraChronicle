public interface IGameStateNetworkBridge
{
    void SendPhaseTransitionRpc(GameplayPhase phase);
    void SendSetReadyRpc(bool ready);
}
