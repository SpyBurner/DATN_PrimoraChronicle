using Zenject;

internal class HandController : IHandController
{
    private readonly IHandModel _model;
    private readonly IDebugLogger _debugLogger;
    private IHandNetworkBridge _bridge;

    public HandController(IHandModel model, IDebugLogger debugLogger)
    {
        _model = model;
        _debugLogger = debugLogger;
    }

    public void Initialize() { }
    public void Dispose() { }

    public void RegisterBridge(IHandNetworkBridge bridge)
    {
        _bridge = bridge;
        _debugLogger.Log($"[HandController] Bridge {(_bridge == null ? "unregistered" : "registered")}.");
    }

    public void PlayCard(string cardId)
    {
        if (_bridge != null)
        {
            _bridge.SendPlayCardRpc(cardId);
        }
        else
        {
            _debugLogger.Log($"HandController: PlayCard {cardId} (Local)");
        }
    }

    public void OnAuthoritativeStateReceived(HandStateData data)
    {
        _model.ApplyState(data);
    }
}
