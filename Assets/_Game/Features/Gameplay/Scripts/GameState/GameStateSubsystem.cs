using System;
using UnityEngine.Events;
using Zenject;

public class GameStateSubsystem : IGameStateSubsystem
{
    [Inject]
    private readonly IGameStateController _controller;
    [Inject]
    private readonly IGameStateModel _model;

    public event UnityAction<int> TurnChanged;
    public event UnityAction<string> PhaseChanged;
    public event UnityAction<int> TimerChanged;

    public void Initialize()
    {
        _model.CurrentTurn.OnChanged += HandleTurnChanged;
        _model.CurrentPhase.OnChanged += HandlePhaseChanged;
        _model.MatchTimer.OnChanged += HandleTimerChanged;
    }

    public void Dispose()
    {
        _model.CurrentTurn.OnChanged -= HandleTurnChanged;
        _model.CurrentPhase.OnChanged -= HandlePhaseChanged;
        _model.MatchTimer.OnChanged -= HandleTimerChanged;
        
        _controller.Dispose();
        _model.Dispose();
    }

    // ── Intent ──────────────────────────────────────────────────────────

    public void StartMatch() => _controller.StartMatch();
    public void EndTurn() => _controller.EndTurn();
    public void SetPhase(string phase) => _controller.SetPhase(phase);

    // ── Network registration ─────────────────────────────────────────────

    public void RegisterNetworkBridge(IGameStateNetworkBridge bridge) 
        => _controller.RegisterBridge(bridge);

    // ── Authoritative sync ───────────────────────────────────────────────

    public void OnAuthoritativeStateReceived(GameStateStateData data) 
        => _controller.OnAuthoritativeStateReceived(data);

    // ── Observable handlers ──────────────────────────────────────────────

    private void HandleTurnChanged() => TurnChanged?.Invoke(_model.CurrentTurn.Value);
    private void HandlePhaseChanged() => PhaseChanged?.Invoke(_model.CurrentPhase.Value);
    private void HandleTimerChanged() => TimerChanged?.Invoke(_model.MatchTimer.Value);
}
