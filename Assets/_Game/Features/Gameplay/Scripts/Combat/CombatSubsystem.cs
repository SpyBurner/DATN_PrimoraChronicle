using System;
using UnityEngine.Events;
using Zenject;

public class CombatSubsystem : ICombatSubsystem
{
    private readonly ICombatController _controller;
    private readonly ICombatModel _model;

    public event UnityAction<string> AttackerChanged;
    public event UnityAction<string> DefenderChanged;
    public event UnityAction<string> LogChanged;

    public CombatSubsystem(ICombatController controller, ICombatModel model)
    {
        _controller = controller;
        _model = model;
    }

    public void Initialize()
    {
        _model.CurrentAttackerId.OnChanged += HandleAttackerChanged;
        _model.CurrentDefenderId.OnChanged += HandleDefenderChanged;
        _model.CombatLog.OnChanged += HandleLogChanged;
    }

    public void Dispose()
    {
        _model.CurrentAttackerId.OnChanged -= HandleAttackerChanged;
        _model.CurrentDefenderId.OnChanged -= HandleDefenderChanged;
        _model.CombatLog.OnChanged -= HandleLogChanged;
        
        _controller.Dispose();
        _model.Dispose();
    }

    // ── Intent ──────────────────────────────────────────────────────────

    public void ExecuteTurn() => _controller.ExecuteTurn();
    public void SkipCombat() => _controller.SkipCombat();

    // ── Network registration ─────────────────────────────────────────────

    public void RegisterNetworkBridge(ICombatNetworkBridge bridge) 
        => _controller.RegisterBridge(bridge);

    // ── Authoritative sync ───────────────────────────────────────────────

    public void OnAuthoritativeStateReceived(CombatStateData data) 
        => _controller.OnAuthoritativeStateReceived(data);

    // ── Observable handlers ──────────────────────────────────────────────

    private void HandleAttackerChanged() => AttackerChanged?.Invoke(_model.CurrentAttackerId.Value);
    private void HandleDefenderChanged() => DefenderChanged?.Invoke(_model.CurrentDefenderId.Value);
    private void HandleLogChanged() => LogChanged?.Invoke(_model.CombatLog.Value);
}
