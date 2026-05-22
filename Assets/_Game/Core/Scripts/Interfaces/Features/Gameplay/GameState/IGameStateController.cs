public interface IGameStateController : IController
{
    void RequestSetLocalReady(bool ready);
    void RegisterBridge(IGameStateNetworkBridge bridge);
    void OnAuthoritativeStateReceived(GameStateData data);
}
