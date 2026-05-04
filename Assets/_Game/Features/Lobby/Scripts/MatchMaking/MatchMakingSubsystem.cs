using System;
using System.Threading.Tasks;
using UnityEngine.Events;
using UnityObservables;
using Zenject;

public class MatchMakingSubsystem : IMatchMakingSubsystem, IInitializable, IDisposable
{
    [Inject] private readonly IMatchMakingController _controller;
    [Inject] private readonly IMatchMakingModel _model;

    public event UnityAction<bool> IsSearchingChanged;
    public event UnityAction<string> StatusChanged;
    public event UnityAction<int> QueuePositionChanged;

    public void Initialize()
    {
        if (_model?.IsSearching != null)
            _model.IsSearching.OnChanged += HandleIsSearchingChanged;

        if (_model?.Status != null)
            _model.Status.OnChanged += HandleStatusChanged;

        if (_model?.QueuePosition != null)
            _model.QueuePosition.OnChanged += HandleQueuePositionChanged;
    }

    public void Dispose()
    {
        if (_model?.IsSearching != null)
            _model.IsSearching.OnChanged -= HandleIsSearchingChanged;

        if (_model?.Status != null)
            _model.Status.OnChanged -= HandleStatusChanged;

        if (_model?.QueuePosition != null)
            _model.QueuePosition.OnChanged -= HandleQueuePositionChanged;
    }

    public Task StartMatchmaking() => _controller.StartMatchmaking();
    public Task CancelMatchmaking() => _controller.CancelMatchmaking();

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
}
