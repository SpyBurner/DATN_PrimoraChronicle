using System;
using UnityEngine.Events;
using Zenject;

public class MatchResultSubsystem : IMatchResultSubsystem
{
    [Inject]
    private readonly IMatchResultController _controller;
    [Inject]
    private readonly IMatchResultModel _model;

    public event UnityAction<bool> IsVictoryChanged;
    public event UnityAction<int> GoldEarnedChanged;
    public event UnityAction<int> RankProgressChanged;
    public void Initialize()
    {
        _model.IsVictory.OnChanged += HandleVictoryChanged;
        _model.GoldEarned.OnChanged += HandleGoldChanged;
        _model.RankProgress.OnChanged += HandleRankChanged;
    }

    public void Dispose()
    {
        _model.IsVictory.OnChanged -= HandleVictoryChanged;
        _model.GoldEarned.OnChanged -= HandleGoldChanged;
        _model.RankProgress.OnChanged -= HandleRankChanged;
        
        _controller.Dispose();
        _model.Dispose();
    }

    // ── Intent ──────────────────────────────────────────────────────────

    public void ShowResult(bool victory, int gold, int rank) => _controller.ShowResult(victory, gold, rank);
    public void BackToLobby() => _controller.BackToLobby();

    // ── Network registration ─────────────────────────────────────────────

    public void RegisterNetworkBridge(IMatchResultNetworkBridge bridge) 
        => _controller.RegisterBridge(bridge);

    // ── Authoritative sync ───────────────────────────────────────────────

    public void OnAuthoritativeStateReceived(MatchResultStateData data) 
        => _controller.OnAuthoritativeStateReceived(data);

    // ── Observable handlers ──────────────────────────────────────────────

    private void HandleVictoryChanged() => IsVictoryChanged?.Invoke(_model.IsVictory.Value);
    private void HandleGoldChanged() => GoldEarnedChanged?.Invoke(_model.GoldEarned.Value);
    private void HandleRankChanged() => RankProgressChanged?.Invoke(_model.RankProgress.Value);
}
