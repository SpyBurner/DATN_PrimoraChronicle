public interface IGameStateController : IController
{
    void RegisterBridge(IGameStateNetworkBridge bridge);
    void OnAuthoritativeStateReceived(GameStateData data);
}
