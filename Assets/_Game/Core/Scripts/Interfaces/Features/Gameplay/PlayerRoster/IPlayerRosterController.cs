using Fusion;

internal interface IPlayerRosterController : IController
{
    void RegisterBridge(IPlayerRosterNetworkBridge bridge);
    void OnAuthoritativeStateReceived(PlayerRosterPublicData data);
}
