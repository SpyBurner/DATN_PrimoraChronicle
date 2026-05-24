using Fusion;

public interface IPlayerRosterController : IController
{
    void RegisterBridge(PlayerRef owner, IPlayerRosterNetworkBridge bridge);
    void OnAuthoritativeStateReceived(PlayerRosterPublicData data);
}
