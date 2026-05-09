using Zenject;

internal class FusePhaseController : IFusePhaseController
{
    private readonly IFusePhaseModel _model;
    private readonly IDebugLogger _debugLogger;
    private IFusePhaseNetworkBridge _bridge;

    public FusePhaseController(IFusePhaseModel model, IDebugLogger debugLogger)
    {
        _model = model;
        _debugLogger = debugLogger;
    }

    public void Initialize() { }
    public void Dispose() { }

    public void RegisterBridge(IFusePhaseNetworkBridge bridge)
    {
        _bridge = bridge;
        _debugLogger.Log($"[FusePhaseController] Bridge {(_bridge == null ? "unregistered" : "registered")}.");
    }

    public void SetUnits(string primaryId, string secondaryId)
    {
        if (_bridge != null)
        {
            _bridge.SendSetUnitsRpc(primaryId, secondaryId);
        }
        else
        {
            _debugLogger.Log($"FusePhaseController: SetUnits {primaryId}, {secondaryId} (Local)");
        }
    }

    public void Fuse()
    {
        if (_bridge != null)
        {
            _bridge.SendFuseRpc();
        }
        else
        {
            _debugLogger.Log("FusePhaseController: Fuse (Local)");
        }
    }

    public void Cancel()
    {
        if (_bridge != null)
        {
            _bridge.SendCancelRpc();
        }
        else
        {
            _debugLogger.Log("FusePhaseController: Cancel (Local)");
        }
    }

    public void OnAuthoritativeStateReceived(FusePhaseStateData data)
    {
        _model.ApplyState(data);
    }
}
