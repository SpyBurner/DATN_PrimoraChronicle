using System;
using System.Collections.Generic;
using UnityEngine.Events;
using Zenject;

public class StartPhaseSubsystem : IStartPhaseSubsystem
{
    private readonly IStartPhaseController _controller;
    private readonly IStartPhaseModel _model;

    public event UnityAction<List<int>> SelectedChampionsChanged;
    public event UnityAction<bool> IsReadyChanged;
    public event UnityAction<string> StatusChanged;

    public StartPhaseSubsystem(IStartPhaseController controller, IStartPhaseModel model)
    {
        _controller = controller;
        _model = model;
    }

    public void Initialize()
    {
        _model.SelectedChampions.OnChanged += HandleChampionsChanged;
        _model.IsReady.OnChanged += HandleIsReadyChanged;
        _model.Status.OnChanged += HandleStatusChanged;
    }

    public void Dispose()
    {
        _model.SelectedChampions.OnChanged -= HandleChampionsChanged;
        _model.IsReady.OnChanged -= HandleIsReadyChanged;
        _model.Status.OnChanged -= HandleStatusChanged;
        
        _controller.Dispose();
        _model.Dispose();
    }

    // ── Intent ──────────────────────────────────────────────────────────

    public void SetIsReady(bool ready) => _controller.SetIsReady(ready);
    public void AddChampion(int championId) => _controller.AddChampion(championId);
    public void RemoveChampion(int championId) => _controller.RemoveChampion(championId);

    // ── Network registration ─────────────────────────────────────────────

    public void RegisterNetworkBridge(IStartPhaseNetworkBridge bridge) 
        => _controller.RegisterBridge(bridge);

    // ── Authoritative sync ───────────────────────────────────────────────

    public void OnAuthoritativeStateReceived(StartPhaseStateData data) 
        => _controller.OnAuthoritativeStateReceived(data);

    // ── Observable handlers ──────────────────────────────────────────────

    private void HandleChampionsChanged() => SelectedChampionsChanged?.Invoke(_model.SelectedChampions.Value);
    private void HandleIsReadyChanged() => IsReadyChanged?.Invoke(_model.IsReady.Value);
    private void HandleStatusChanged() => StatusChanged?.Invoke(_model.Status.Value);
}
