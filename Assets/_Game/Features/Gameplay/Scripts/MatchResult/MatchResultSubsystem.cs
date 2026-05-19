using System;
using System.Threading.Tasks;
using UnityEngine.Events;
using Zenject;

public class MatchResultSubsystem : IMatchResultSubsystem
{
    [Inject] private readonly IMatchResultController _controller;
    [Inject] private readonly IMatchResultModel _model;

    public event UnityAction<GameMatchResult> MatchEnded;

    public bool HasResult => _model.HasResult.Value;
    public GameMatchResult Result => _model.Result.Value;

    public void Initialize()
    {
        _model.HasResult.OnChanged += HandleHasResultChanged;
        _controller.Initialize();
    }

    public void Dispose()
    {
        _model.HasResult.OnChanged -= HandleHasResultChanged;
        _controller.Dispose();
        _model.Dispose();
    }

    public Task ReturnToLobby() => _controller.ReturnToLobby();

    public void RegisterNetworkBridge(IMatchResultNetworkBridge bridge) => _controller.RegisterBridge(bridge);
    public void OnAuthoritativeStateReceived(GameMatchResult data) => _controller.OnAuthoritativeStateReceived(data);

    private void HandleHasResultChanged()
    {
        if (!_model.HasResult.Value) return;
        try { MatchEnded?.Invoke(_model.Result.Value); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }
}
