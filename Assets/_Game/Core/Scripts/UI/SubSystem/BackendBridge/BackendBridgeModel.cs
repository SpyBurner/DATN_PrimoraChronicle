using UnityObservables;

public class BackendBridgeModel : IBackendBridgeModel
{
    private readonly Observable<StartSessionCommand> _pendingStartSession = new(null);
    private readonly Observable<bool> _isListening = new(false);

    public Observable<StartSessionCommand> PendingStartSession => _pendingStartSession;
    public Observable<bool> IsListening => _isListening;

    public void Initialize()
    {
        _pendingStartSession.Value = null;
        _isListening.Value = false;
    }

    public void Dispose()
    {
        _pendingStartSession.Value = null;
        _isListening.Value = false;
    }

    public void ApplyState(BackendBridgeStateData data)
    {
        _pendingStartSession.Value = data.PendingStartSession;
        _isListening.Value = data.IsListening;
    }
}
