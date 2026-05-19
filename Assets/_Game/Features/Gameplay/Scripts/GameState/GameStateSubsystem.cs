using System;
using Fusion;
using UnityEngine.Events;
using Zenject;

public class GameStateSubsystem : IGameStateSubsystem
{
    [Inject] private readonly IGameStateController _controller;
    [Inject] private readonly IGameStateModel _model;

    public event UnityAction<GameplayPhase> PhaseChanged;
    public event UnityAction<float> PhaseTimeRemainingChanged;
    public event UnityAction<float> MatchElapsedChanged;
    public event UnityAction<int> RoundNumberChanged;
    public event UnityAction<PlayerRef> CurrentCombatActorChanged;

    public GameplayPhase Phase => _model.Phase.Value;
    public float PhaseTimeRemaining => _model.PhaseTimeRemaining.Value;
    public float MatchElapsed => _model.MatchElapsed.Value;
    public int RoundNumber => _model.RoundNumber.Value;
    public PlayerRef CurrentCombatActor => _model.CurrentCombatActor.Value;

    public void Initialize()
    {
        _model.Phase.OnChanged += HandlePhaseChanged;
        _model.PhaseTimeRemaining.OnChanged += HandlePhaseTimeRemainingChanged;
        _model.MatchElapsed.OnChanged += HandleMatchElapsedChanged;
        _model.RoundNumber.OnChanged += HandleRoundNumberChanged;
        _model.CurrentCombatActor.OnChanged += HandleCurrentCombatActorChanged;
        _controller.Initialize();
    }

    public void Dispose()
    {
        _model.Phase.OnChanged -= HandlePhaseChanged;
        _model.PhaseTimeRemaining.OnChanged -= HandlePhaseTimeRemainingChanged;
        _model.MatchElapsed.OnChanged -= HandleMatchElapsedChanged;
        _model.RoundNumber.OnChanged -= HandleRoundNumberChanged;
        _model.CurrentCombatActor.OnChanged -= HandleCurrentCombatActorChanged;
        _controller.Dispose();
        _model.Dispose();
    }

    public void RegisterNetworkBridge(IGameStateNetworkBridge bridge) => _controller.RegisterBridge(bridge);
    public void OnAuthoritativeStateReceived(GameStateData data) => _controller.OnAuthoritativeStateReceived(data);

    private void HandlePhaseChanged()
    {
        try { PhaseChanged?.Invoke(_model.Phase.Value); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }

    private void HandlePhaseTimeRemainingChanged()
    {
        try { PhaseTimeRemainingChanged?.Invoke(_model.PhaseTimeRemaining.Value); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }

    private void HandleMatchElapsedChanged()
    {
        try { MatchElapsedChanged?.Invoke(_model.MatchElapsed.Value); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }

    private void HandleRoundNumberChanged()
    {
        try { RoundNumberChanged?.Invoke(_model.RoundNumber.Value); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }

    private void HandleCurrentCombatActorChanged()
    {
        try { CurrentCombatActorChanged?.Invoke(_model.CurrentCombatActor.Value); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }
}
