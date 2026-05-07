using System;
using System.Threading.Tasks;
using UnityEngine.Events;
using UnityObservables;
using Zenject;

public class MatchMakingSubsystem : IMatchMakingSubsystem
{
    [Inject] private readonly IMatchMakingController _controller;
    [Inject] private readonly IMatchMakingModel _model;

    public event UnityAction<bool> IsSearchingChanged;
    public event UnityAction<string> StatusChanged;
    public event UnityAction<int> QueuePositionChanged;
    public event UnityAction<bool> IsMatchFoundChanged;
    public event UnityAction<int> ConfirmationTimerChanged;
    
    public bool IsSearching => _model.IsSearching.Value;
    public string Status => _model.Status.Value;
    public int QueuePosition => _model.QueuePosition.Value;
    public bool IsMatchFound => _model.IsMatchFound.Value;
    public int ConfirmationTimer => _model.ConfirmationTimer.Value;

    public void Initialize()
    {
        if (_model?.IsSearching != null)
            _model.IsSearching.OnChanged += HandleIsSearchingChanged;

        if (_model?.Status != null)
            _model.Status.OnChanged += HandleStatusChanged;

        if (_model?.QueuePosition != null)
            _model.QueuePosition.OnChanged += HandleQueuePositionChanged;

        if (_model?.IsMatchFound != null)
            _model.IsMatchFound.OnChanged += HandleIsMatchFoundChanged;

        if (_model?.ConfirmationTimer != null)
            _model.ConfirmationTimer.OnChanged += HandleConfirmationTimerChanged;
    }

    public void Dispose()
    {
        if (_model?.IsSearching != null)
            _model.IsSearching.OnChanged -= HandleIsSearchingChanged;

        if (_model?.Status != null)
            _model.Status.OnChanged -= HandleStatusChanged;

        if (_model?.QueuePosition != null)
            _model.QueuePosition.OnChanged -= HandleQueuePositionChanged;

        if (_model?.IsMatchFound != null)
            _model.IsMatchFound.OnChanged -= HandleIsMatchFoundChanged;

        if (_model?.ConfirmationTimer != null)
            _model.ConfirmationTimer.OnChanged -= HandleConfirmationTimerChanged;
    }

    public Task StartMatchmaking() => _controller.StartMatchmaking();
    public Task CancelMatchmaking() => _controller.CancelMatchmaking();
    public Task AcceptMatch() => _controller.AcceptMatch();
    public Task RejectMatch() => _controller.RejectMatch();

    private void HandleIsSearchingChanged()
    {
        try { IsSearchingChanged?.Invoke(_model.IsSearching.Value); } catch { }
    }

    private void HandleStatusChanged()
    {
        try { StatusChanged?.Invoke(_model.Status.Value); } catch { }
    }

    private void HandleQueuePositionChanged()
    {
        try { QueuePositionChanged?.Invoke(_model.QueuePosition.Value); } catch { }
    }

    private void HandleIsMatchFoundChanged()
    {
        try { IsMatchFoundChanged?.Invoke(_model.IsMatchFound.Value); } catch { }
    }

    private void HandleConfirmationTimerChanged()
    {
        try { ConfirmationTimerChanged?.Invoke(_model.ConfirmationTimer.Value); } catch { }
    }
}
