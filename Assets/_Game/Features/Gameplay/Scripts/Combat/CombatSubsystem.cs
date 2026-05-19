using System;
using System.Collections.Generic;
using UnityEngine.Events;
using Zenject;

public class CombatSubsystem : ICombatSubsystem
{
    [Inject] private readonly ICombatController _controller;
    [Inject] private readonly ICombatModel _model;

    public event UnityAction<IReadOnlyList<string>> QueueChanged;
    public event UnityAction<string> CurrentTurnChanged;
    public event UnityAction TurnEnded;
    public event UnityAction CombatPhaseEnded;

    public IReadOnlyList<string> ActionQueue => _model.ActionQueue;
    public string CurrentActorId => _model.CurrentActorId.Value;
    public bool IsCombatActive => _model.IsCombatActive.Value;

    public void Initialize()
    {
        _model.QueueChanged += HandleQueueChanged;
        _model.CurrentActorId.OnChanged += HandleCurrentActorChanged;
        _controller.Initialize();
    }

    public void Dispose()
    {
        _model.QueueChanged -= HandleQueueChanged;
        _model.CurrentActorId.OnChanged -= HandleCurrentActorChanged;
        _controller.Dispose();
        _model.Dispose();
    }

    public void RequestMove(string unitId, HexCoord destination) => _controller.RequestMove(unitId, destination);
    public void RequestNormalAttack(string unitId, HexCoord target) => _controller.RequestNormalAttack(unitId, target);
    public void RequestSkill(string unitId, string skillId, HexCoord target) => _controller.RequestSkill(unitId, skillId, target);
    public void RequestEndTurn() => _controller.RequestEndTurn();

    public void RegisterNetworkBridge(ICombatNetworkBridge bridge) => _controller.RegisterBridge(bridge);
    public void OnAuthoritativeStateReceived(CombatStateData data) => _controller.OnAuthoritativeStateReceived(data);

    private void HandleQueueChanged()
    {
        try { QueueChanged?.Invoke(_model.ActionQueue); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }

    private void HandleCurrentActorChanged()
    {
        try { CurrentTurnChanged?.Invoke(_model.CurrentActorId.Value); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }
}
