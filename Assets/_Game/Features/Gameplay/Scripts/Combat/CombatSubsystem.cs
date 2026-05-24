using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine.Events;
using Zenject;

public class CombatSubsystem : ICombatSubsystem
{
    [Inject] private readonly ICombatController _controller;
    [Inject] private readonly ICombatModel _model;

    public event UnityAction<IReadOnlyList<CombatQueueEntry>> QueueChanged;
    public event UnityAction<NetworkId> CurrentTurnChanged;
    public event UnityAction TurnEnded;
    public event UnityAction<bool> CurrentActorCanMoveChanged;
    public event UnityAction<bool> CurrentActorCanActChanged;

    public IReadOnlyList<CombatQueueEntry> ActionQueue => _model.ActionQueue;
    public NetworkId CurrentActor => _model.CurrentActor.Value;
    public bool CurrentActorCanMove => !_model.HasMoved.Value;
    public bool CurrentActorCanAct => !_model.HasActed.Value;

    public void Initialize()
    {
        _model.QueueChanged += HandleQueueChanged;
        _model.CurrentActor.OnChanged += HandleCurrentActorChanged;
        _model.HasMoved.OnChanged += HandleHasMovedChanged;
        _model.HasActed.OnChanged += HandleHasActedChanged;
        _controller.Initialize();
    }

    public void Dispose()
    {
        _model.QueueChanged -= HandleQueueChanged;
        _model.CurrentActor.OnChanged -= HandleCurrentActorChanged;
        _model.HasMoved.OnChanged -= HandleHasMovedChanged;
        _model.HasActed.OnChanged -= HandleHasActedChanged;
        _controller.Dispose();
        _model.Dispose();
    }

    public void RequestMove(NetworkId unit, HexCoord destination) => _controller.RequestMove(unit, destination);
    public void RequestNormalAttack(NetworkId unit, HexCoord target) => _controller.RequestNormalAttack(unit, target);
    public void RequestSkill(NetworkId unit, string skillId, HexCoord target) => _controller.RequestSkill(unit, skillId, target);
    public void EndTurn() => _controller.EndTurn();

    public void RegisterNetworkBridge(ICombatNetworkBridge bridge) => _controller.RegisterBridge(bridge);
    public void OnAuthoritativeStateReceived(CombatStateData data) => _controller.OnAuthoritativeStateReceived(data);

    private void HandleQueueChanged()
    {
        try { QueueChanged?.Invoke(_model.ActionQueue); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }

    private void HandleCurrentActorChanged()
    {
        try { CurrentTurnChanged?.Invoke(_model.CurrentActor.Value); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }

    private void HandleHasMovedChanged()
    {
        try { CurrentActorCanMoveChanged?.Invoke(!_model.HasMoved.Value); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }

    private void HandleHasActedChanged()
    {
        try { CurrentActorCanActChanged?.Invoke(!_model.HasActed.Value); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }
}
