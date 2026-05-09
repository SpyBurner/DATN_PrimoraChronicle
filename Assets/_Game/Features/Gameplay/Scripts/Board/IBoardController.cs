internal interface IBoardController : IController
{
    void PlaceUnit(int cellIndex, string unitId);
    void RegisterBridge(IBoardNetworkBridge bridge);
    void OnAuthoritativeStateReceived(BoardStateData data);
}
