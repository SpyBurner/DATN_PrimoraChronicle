public interface IUnitController : IController
{
    void RegisterBridge(IUnitNetworkBridge bridge);
    void OnUnitStateReceived(UnitStateData data);
    void OnUnitDestroyed(string unitNetworkId);
}
