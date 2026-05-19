using Zenject;

internal class UnitController : IUnitController
{
    [Inject] private readonly IUnitModel _model;
    [Inject] private readonly IDebugLogger _logger;

    private IUnitNetworkBridge _bridge;

    public void Initialize() { }

    public void Dispose() => _bridge = null;

    public void RegisterBridge(IUnitNetworkBridge bridge)
    {
        _bridge = bridge;
        _logger.Log($"[Unit] Bridge {(bridge == null ? "unregistered" : "registered")}.");
    }

    public void OnUnitStateReceived(UnitStateData data) => _model.ApplyUnitState(data);

    public void OnUnitDestroyed(string unitNetworkId) => _model.RemoveUnit(unitNetworkId);
}
