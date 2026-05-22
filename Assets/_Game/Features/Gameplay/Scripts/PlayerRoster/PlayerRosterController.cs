using Zenject;

internal class PlayerRosterController : IPlayerRosterController
{
    [Inject] private readonly IPlayerRosterModel _model;
    [Inject] private readonly IDebugLogger _logger;

    private IPlayerRosterNetworkBridge _bridge;

    public void Initialize() { }

    public void Dispose() => _bridge = null;

    public void RegisterBridge(IPlayerRosterNetworkBridge bridge)
    {
        _bridge = bridge;
        _logger.Log($"[PlayerRoster] Bridge {(bridge == null ? "unregistered" : "registered")}.");
    }

    public void OnAuthoritativeStateReceived(PlayerRosterPublicData data) => _model.ApplyState(data);
}
