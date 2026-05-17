using System.Threading.Tasks;
using UnityEngine.Events;
using Zenject;

public class BackendBridgeSubsystem : IBackendBridgeSubsystem
{
    [Inject] private readonly IBackendBridgeModel _model;
    [Inject] private readonly IBackendBridgeController _controller;

    public event UnityAction<StartSessionCommand> StartSessionReceived;
    public event UnityAction ForceEndMatchReceived;

    public void Initialize()
    {
        _model.PendingStartSession.OnChanged += HandlePendingStartSessionChanged;
    }

    public void Dispose()
    {
        _model.PendingStartSession.OnChanged -= HandlePendingStartSessionChanged;
    }

    private void HandlePendingStartSessionChanged()
    {
        if (_model.PendingStartSession.Value != null)
        {
            StartSessionReceived?.Invoke(_model.PendingStartSession.Value);
            _controller.ClearPendingStartSession();
        }
    }

    public Task ReportMatchResultAsync(MatchResultData result)
    {
        return _controller.ReportMatchResultAsync(result);
    }

    public Task ReportPlayerDisconnectedAsync(string userId)
    {
        return _controller.ReportPlayerDisconnectedAsync(userId);
    }
}
