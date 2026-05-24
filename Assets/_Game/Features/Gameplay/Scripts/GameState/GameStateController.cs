using Zenject;

internal class GameStateController : IGameStateController
{
    [Inject] private readonly IGameStateModel _model;
    [Inject] private readonly IDebugLogger _logger;

    private IGameStateNetworkBridge _bridge;

    public void Initialize() { }

    public void Dispose() => _bridge = null;

    public void RegisterBridge(IGameStateNetworkBridge bridge)
    {
        _bridge = bridge;
        _logger.Log("LOG_GAMESTATE", nameof(GameStateController), $"Bridge {(bridge == null ? "unregistered" : "registered")}.");
    }

    public void RequestSetLocalReady(bool ready) => _bridge?.SendSetReadyRpc(ready);

    public void OnAuthoritativeStateReceived(GameStateData data) => _model.ApplyState(data);
}
