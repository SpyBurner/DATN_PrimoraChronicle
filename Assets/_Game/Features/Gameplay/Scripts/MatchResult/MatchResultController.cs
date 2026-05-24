using System.Threading.Tasks;
using Fusion;
using Zenject;

internal class MatchResultController : IMatchResultController
{
    [Inject] private readonly IMatchResultModel _model;
    [Inject] private readonly ISceneLoaderSubsystem _sceneLoader;
    [Inject] private readonly IBackendBridgeSubsystem _backendBridge;
    [Inject] private readonly IAuthSessionSubsystem _authSession;
    [Inject] private readonly INetworkManagerSubsystem _networkManager;
    [Inject] private readonly IDebugLogger _logger;

    private IMatchResultNetworkBridge _bridge;
    private bool _reportSent;

    public void Initialize() { }

    public void Dispose()
    {
        _bridge = null;
        _reportSent = false;
    }

    public void RegisterBridge(IMatchResultNetworkBridge bridge)
    {
        _bridge = bridge;
        _logger.Log("LOG_MATCHRESULT", nameof(MatchResultController), $"Bridge {(bridge == null ? "unregistered" : "registered")}.");
    }

    public void OnAuthoritativeStateReceived(GameMatchResult data)
    {
        _model.ApplyState(data);
        ReportToBackend(data);
    }

    public async Task ReturnToLobby()
    {
        await _networkManager.ShutdownRunner();
        await _sceneLoader.LoadScene("Lobby");
    }

    private async void ReportToBackend(GameMatchResult data)
    {
        if (_reportSent) return;

        var runner = _networkManager.Runner;
        if (runner == null || !runner.IsServer) return;

        _reportSent = true;

        string winnerUserId = null;
        string loserUserId = null;

        foreach (var player in runner.ActivePlayers)
        {
            if (player == data.Winner)
                winnerUserId = ResolveUserId(player);
            else
                loserUserId = ResolveUserId(player);
        }

        var reportData = new MatchResultData
        {
            SessionName = _networkManager.SessionName,
            WinnerUserId = data.IsTie ? null : winnerUserId,
            LoserUserId = data.IsTie ? null : loserUserId,
            DurationSeconds = (int)data.DurationSeconds,
            EndReason = data.IsTie ? "Tie" : "Normal"
        };

        try
        {
            await _backendBridge.ReportMatchResultAsync(reportData);
            _logger.Log("LOG_MATCHRESULT", nameof(MatchResultController), "Backend report sent successfully.");
        }
        catch (System.Exception ex)
        {
            _logger.LogWarning("LOG_MATCHRESULT", nameof(MatchResultController), $"Backend report failed: {ex.Message}");
        }
    }

    private string ResolveUserId(PlayerRef player)
    {
        var runner = _networkManager.Runner;
        if (runner == null) return player.ToString();

        if (runner.LocalPlayer == player)
            return _authSession.UserId;

        return player.ToString();
    }
}
