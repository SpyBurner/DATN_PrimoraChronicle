using Fusion;

public interface IPlayerRosterController : IController
{
    void RegisterBridge(IPlayerRosterNetworkBridge bridge);
    void OnAuthoritativeStateReceived(PlayerRosterPublicData data);
}
