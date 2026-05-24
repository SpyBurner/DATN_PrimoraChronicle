public interface IMatchRewardsController : IController
{
    void RegisterBridge(IMatchRewardsPrivateNetworkBridge bridge);
    void OnAuthoritativeStateReceived(MatchRewardsPrivateData data);
}
