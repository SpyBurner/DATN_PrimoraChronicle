using System;
using UnityEngine.Events;
using Zenject;

public class FusePhaseSubsystem : IFusePhaseSubsystem
{
    [Inject]
    private readonly IFusePhaseController _controller;
    [Inject]
    private readonly IFusePhaseModel _model;

    public event UnityAction<bool> IsActiveChanged;
    public event UnityAction<string> PrimaryUnitIdChanged;
    public event UnityAction<string> SecondaryUnitIdChanged;

    public void Initialize()
    {
        _model.IsActive.OnChanged += HandleIsActiveChanged;
        _model.PrimaryUnitId.OnChanged += HandlePrimaryChanged;
        _model.SecondaryUnitId.OnChanged += HandleSecondaryChanged;
    }

    public void Dispose()
    {
        _model.IsActive.OnChanged -= HandleIsActiveChanged;
        _model.PrimaryUnitId.OnChanged -= HandlePrimaryChanged;
        _model.SecondaryUnitId.OnChanged -= HandleSecondaryChanged;
        
        _controller.Dispose();
        _model.Dispose();
    }

    // ── Intent ──────────────────────────────────────────────────────────

    public void SetUnits(string primaryId, string secondaryId) => _controller.SetUnits(primaryId, secondaryId);
    public void Fuse() => _controller.Fuse();
    public void Cancel() => _controller.Cancel();

    // ── Network registration ─────────────────────────────────────────────

    public void RegisterNetworkBridge(IFusePhaseNetworkBridge bridge) 
        => _controller.RegisterBridge(bridge);

    // ── Authoritative sync ───────────────────────────────────────────────

    public void OnAuthoritativeStateReceived(FusePhaseStateData data) 
        => _controller.OnAuthoritativeStateReceived(data);

    // ── Observable handlers ──────────────────────────────────────────────

    private void HandleIsActiveChanged() => IsActiveChanged?.Invoke(_model.IsActive.Value);
    private void HandlePrimaryChanged() => PrimaryUnitIdChanged?.Invoke(_model.PrimaryUnitId.Value);
    private void HandleSecondaryChanged() => SecondaryUnitIdChanged?.Invoke(_model.SecondaryUnitId.Value);
}
