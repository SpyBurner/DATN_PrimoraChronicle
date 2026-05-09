internal interface IGameStateController : IController
{
    void StartMatch();
    void EndTurn();
    void SetPhase(string phase);
    void RegisterBridge(IGameStateNetworkBridge bridge);
    void OnAuthoritativeStateReceived(GameStateStateData data);
}

