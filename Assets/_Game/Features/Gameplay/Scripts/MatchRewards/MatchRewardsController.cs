using Zenject;

internal class MatchRewardsController : IMatchRewardsController
{
    [Inject] private readonly IMatchRewardsModel _model;
    [Inject] private readonly IDebugLogger _logger;

    private IMatchRewardsPrivateNetworkBridge _bridge;

    public void Initialize() { }

    public void Dispose() => _bridge = null;

    public void RegisterBridge(IMatchRewardsPrivateNetworkBridge bridge)
    {
        _bridge = bridge;
        _logger.Log("LOG_MATCHREWARDS", nameof(MatchRewardsController), $"Bridge {(bridge == null ? "unregistered" : "registered")}.");
    }

    public void OnAuthoritativeStateReceived(MatchRewardsPrivateData data) => _model.ApplyState(data);
}
