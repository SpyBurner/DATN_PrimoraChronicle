using Zenject;

internal class StartPhaseController : IStartPhaseController
{
    private readonly IStartPhaseModel _model;
    private readonly IDebugLogger _debugLogger;
    private IStartPhaseNetworkBridge _bridge;

    public StartPhaseController(IStartPhaseModel model, IDebugLogger debugLogger)
    {
        _model = model;
        _debugLogger = debugLogger;
    }

    public void Initialize() { }
    public void Dispose() { }

    public void RegisterBridge(IStartPhaseNetworkBridge bridge)
    {
        _bridge = bridge;
        _debugLogger.Log($"[StartPhaseController] Bridge {(_bridge == null ? "unregistered" : "registered")}.");
    }

    public void SetIsReady(bool ready)
    {
        if (_bridge != null)
        {
            _bridge.SendSetIsReadyRpc(ready);
        }
        else
        {
            _debugLogger.Log($"StartPhaseController: SetIsReady {ready} (Local)");
        }
    }

    public void AddChampion(int championId)
    {
        if (_bridge != null)
        {
            _bridge.SendAddChampionRpc(championId);
        }
        else
        {
            _debugLogger.Log($"StartPhaseController: AddChampion {championId} (Local)");
        }
    }

    public void RemoveChampion(int championId)
    {
        if (_bridge != null)
        {
            _bridge.SendRemoveChampionRpc(championId);
        }
        else
        {
            _debugLogger.Log($"StartPhaseController: RemoveChampion {championId} (Local)");
        }
    }

    public void OnAuthoritativeStateReceived(StartPhaseStateData data)
    {
        _model.ApplyState(data);
    }
}
