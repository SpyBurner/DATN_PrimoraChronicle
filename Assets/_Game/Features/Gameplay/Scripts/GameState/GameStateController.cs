using Zenject;

internal class GameStateController : IGameStateController
{
    private readonly IGameStateModel _model;
    private readonly IDebugLogger _debugLogger;
    private IGameStateNetworkBridge _bridge;

    public GameStateController(IGameStateModel model, IDebugLogger debugLogger)
    {
        _model = model;
        _debugLogger = debugLogger;
    }

    public void Initialize() { }
    public void Dispose() { }

    public void RegisterBridge(IGameStateNetworkBridge bridge)
    {
        _bridge = bridge;
        _debugLogger.Log($"[GameStateController] Bridge {(_bridge == null ? "unregistered" : "registered")}.");
    }

    public void StartMatch()
    {
        if (_bridge != null)
        {
            _bridge.SendStartMatchRpc();
        }
        else
        {
            _debugLogger.Log("GameStateController: StartMatch (Local)");
        }
    }

    public void EndTurn()
    {
        if (_bridge != null)
        {
            _bridge.SendEndTurnRpc();
        }
        else
        {
            _debugLogger.Log("GameStateController: EndTurn (Local)");
        }
    }

    public void SetPhase(string phase)
    {
        if (_bridge != null)
        {
            _bridge.SendSetPhaseRpc(phase);
        }
        else
        {
            _debugLogger.Log($"GameStateController: SetPhase {phase} (Local)");
        }
    }

    public void OnAuthoritativeStateReceived(GameStateStateData data)
    {
        _model.ApplyState(data);
    }
}
