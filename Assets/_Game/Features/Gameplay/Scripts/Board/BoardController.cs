using Zenject;

internal class BoardController : IBoardController
{
    [Inject] private readonly IBoardModel _model;
    [Inject] private readonly IDebugLogger _logger;

    private IBoardNetworkBridge _bridge;

    public void Initialize() { }

    public void Dispose() => _bridge = null;

    public void RegisterBridge(IBoardNetworkBridge bridge)
    {
        _bridge = bridge;
        _logger.Log($"[Board] Bridge {(bridge == null ? "unregistered" : "registered")}.");
    }

    public void OnAuthoritativeStateReceived(BoardStateData data) => _model.ApplyState(data);
}
