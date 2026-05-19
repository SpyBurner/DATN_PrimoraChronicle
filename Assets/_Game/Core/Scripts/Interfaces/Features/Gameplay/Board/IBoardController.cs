public interface IBoardController : IController
{
    void RegisterBridge(IBoardNetworkBridge bridge);
    void OnAuthoritativeStateReceived(BoardStateData data);
}
