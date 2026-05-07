using System;
using UnityEngine.Events;
using Zenject;

public class GameStateSubsystem : IGameStateSubsystem
{
    [Inject] private readonly IGameStateController _controller;
    [Inject] private readonly IGameStateModel _model;

    public IGameStateModel Model => _model;
    public IGameStateController Controller => _controller;

    public event UnityAction<int> CurrentTurnChanged;
    public event UnityAction<string> CurrentPhaseChanged;
    public event UnityAction<int> MatchTimerChanged;

    public void Initialize()
    {
        if (_model?.CurrentTurn != null)
            _model.CurrentTurn.OnChanged += HandleCurrentTurnChanged;

        if (_model?.CurrentPhase != null)
            _model.CurrentPhase.OnChanged += HandleCurrentPhaseChanged;

        if (_model?.MatchTimer != null)
            _model.MatchTimer.OnChanged += HandleMatchTimerChanged;
    }

    public void Dispose()
    {
        if (_model?.CurrentTurn != null)
            _model.CurrentTurn.OnChanged -= HandleCurrentTurnChanged;

        if (_model?.CurrentPhase != null)
            _model.CurrentPhase.OnChanged -= HandleCurrentPhaseChanged;

        if (_model?.MatchTimer != null)
            _model.MatchTimer.OnChanged -= HandleMatchTimerChanged;
    }

    public void StartMatch() => _controller.StartMatch();
    public void EndTurn() => _controller.EndTurn();

    private void HandleCurrentTurnChanged()
    {
        try { CurrentTurnChanged?.Invoke(_model.CurrentTurn.Value); } catch { }
    }

    private void HandleCurrentPhaseChanged()
    {
        try { CurrentPhaseChanged?.Invoke(_model.CurrentPhase.Value); } catch { }
    }

    private void HandleMatchTimerChanged()
    {
        try { MatchTimerChanged?.Invoke(_model.MatchTimer.Value); } catch { }
    }
}
