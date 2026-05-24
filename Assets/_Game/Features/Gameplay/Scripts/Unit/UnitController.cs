using Fusion;
using Zenject;

internal class UnitController : IUnitController
{
    [Inject] private readonly IUnitModel _model;
    [Inject] private readonly IDebugLogger _logger;

    private IUnitPublicNetworkBridge _publicBridge;
    private IUnitPrivateNetworkBridge _privateBridge;

    public void Initialize() { }

    public void Dispose()
    {
        _publicBridge = null;
        _privateBridge = null;
    }

    public void RegisterPublicBridge(IUnitPublicNetworkBridge bridge)
    {
        _publicBridge = bridge;
        _logger.Log("LOG_UNIT", nameof(UnitController), $"Public bridge {(bridge == null ? "unregistered" : "registered")}.");
    }

    public void RegisterPrivateBridge(IUnitPrivateNetworkBridge bridge)
    {
        _privateBridge = bridge;
        _logger.Log("LOG_UNIT", nameof(UnitController), $"Private bridge {(bridge == null ? "unregistered" : "registered")}.");
    }

    public void OnUnitPublicStateReceived(UnitPublicData data) => _model.ApplyPublicState(data);
    public void OnUnitPrivateStateReceived(UnitPrivateData data) => _model.ApplyPrivateState(data);
    public void OnUnitDestroyed(NetworkId unitId) => _model.RemoveUnit(unitId);
}
