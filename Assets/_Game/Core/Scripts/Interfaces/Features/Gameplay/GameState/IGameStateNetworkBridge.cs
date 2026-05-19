public interface IGameStateNetworkBridge
{
    void SendPhaseTransitionRpc(GameplayPhase phase);
}
