using Fusion;

public interface IUnitController : IController
{
    void RegisterPublicBridge(IUnitPublicNetworkBridge bridge);
    void RegisterPrivateBridge(IUnitPrivateNetworkBridge bridge);
    void OnUnitPublicStateReceived(UnitPublicData data);
    void OnUnitPrivateStateReceived(UnitPrivateData data);
    void OnUnitDestroyed(NetworkId unitId);
}
