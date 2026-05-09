using Zenject;

internal class MatchResultController : IMatchResultController
{
    private readonly IMatchResultModel _model;
    private readonly IDebugLogger _debugLogger;
    private IMatchResultNetworkBridge _bridge;

    public MatchResultController(IMatchResultModel model, IDebugLogger debugLogger)
    {
        _model = model;
        _debugLogger = debugLogger;
    }

    public void Initialize() { }
    public void Dispose() { }

    public void RegisterBridge(IMatchResultNetworkBridge bridge)
    {
        _bridge = bridge;
        _debugLogger.Log($"[MatchResultController] Bridge {(_bridge == null ? "unregistered" : "registered")}.");
    }

    public void ShowResult(bool victory, int gold, int rank)
    {
        if (_bridge != null)
        {
            _bridge.SendShowResultRpc(victory, gold, rank);
        }
        else
        {
            _debugLogger.Log($"MatchResultController: ShowResult {victory}, {gold}, {rank} (Local)");
        }
    }

    public void BackToLobby()
    {
        if (_bridge != null)
        {
            _bridge.SendBackToLobbyRpc();
        }
        else
        {
            _debugLogger.Log("MatchResultController: BackToLobby (Local)");
        }
    }

    public void OnAuthoritativeStateReceived(MatchResultStateData data)
    {
        _model.ApplyState(data);
    }
}
