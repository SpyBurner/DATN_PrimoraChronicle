using UnityObservables;

internal interface IBackendBridgeModel : IModel
{
    Observable<StartSessionCommand> PendingStartSession { get; }
    Observable<bool> IsListening { get; }

    void ApplyState(BackendBridgeStateData data);
}
