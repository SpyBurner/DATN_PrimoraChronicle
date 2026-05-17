using System;
using System.Threading.Tasks;
using UnityEngine.Events;
using UnityObservables;
using Zenject;

public class MatchMakingSubsystem : IMatchMakingSubsystem
{
    [Inject] private readonly IMatchMakingController _controller;
    [Inject] private readonly IMatchMakingModel _model;

    public event UnityAction<string> StatusChanged;
    public event UnityAction<int> TimerChanged;
    
    public void Initialize()
    {
        if (_model?.Status != null)
            _model.Status.OnChanged += HandleStatusChanged;

        if (_model?.Timer != null)
            _model.Timer.OnChanged += HandleConfirmationTimerChanged;
    }

    public void Dispose()
    {
        if (_model?.Timer != null)
            _model.Timer.OnChanged -= HandleConfirmationTimerChanged;
    }

    public Task StartMatchmaking() => _controller.StartMatchmaking();
    public Task CancelMatchmaking() => _controller.CancelMatchmaking();
    public Task AcceptMatch() => _controller.AcceptMatch();
    public Task RejectMatch() => _controller.RejectMatch();

    private void HandleStatusChanged()
    {
        try { StatusChanged?.Invoke(_model.Status.Value); } catch { }
    }

    private void HandleConfirmationTimerChanged()
    {
        try { TimerChanged?.Invoke((int)MathF.Ceiling(_model.Timer.Value)); } catch { }
    }
}
