using Zenject;

internal class DrawPhaseController : IDrawPhaseController
{
    private readonly IDrawPhaseModel _model;
    private readonly IDebugLogger _debugLogger;
    private IDrawPhaseNetworkBridge _bridge;

    public DrawPhaseController(IDrawPhaseModel model, IDebugLogger debugLogger)
    {
        _model = model;
        _debugLogger = debugLogger;
    }

    public void Initialize() { }
    public void Dispose() { }

    public void RegisterBridge(IDrawPhaseNetworkBridge bridge)
    {
        _bridge = bridge;
        _debugLogger.Log($"[DrawPhaseController] Bridge {(_bridge == null ? "unregistered" : "registered")}.");
    }

    public void StartDraw(int count)
    {
        if (_bridge != null)
        {
            _bridge.SendStartDrawRpc(count);
        }
        else
        {
            _debugLogger.Log($"DrawPhaseController: StartDraw {count} (Local)");
        }
    }

    public void CompleteDraw()
    {
        if (_bridge != null)
        {
            _bridge.SendCompleteDrawRpc();
        }
        else
        {
            _debugLogger.Log("DrawPhaseController: CompleteDraw (Local)");
        }
    }

    public void OnAuthoritativeStateReceived(DrawPhaseStateData data)
    {
        _model.ApplyState(data);
    }
}
