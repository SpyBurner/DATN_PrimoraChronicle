using System;
using System.Collections.Generic;
using UnityEngine.Events;
using Zenject;

public class BoardSubsystem : IBoardSubsystem
{
    private readonly IBoardController _controller;
    private readonly IBoardModel _model;

    public event UnityAction<Dictionary<int, string>> GridChanged;

    public BoardSubsystem(IBoardController controller, IBoardModel model)
    {
        _controller = controller;
        _model = model;
    }

    public void Initialize()
    {
        _model.GridOccupancy.OnChanged += HandleGridChanged;
    }

    public void Dispose()
    {
        _model.GridOccupancy.OnChanged -= HandleGridChanged;
        
        _controller.Dispose();
        _model.Dispose();
    }

    // ── Intent ──────────────────────────────────────────────────────────

    public void PlaceUnit(int cellIndex, string unitId) => _controller.PlaceUnit(cellIndex, unitId);

    // ── Network registration ─────────────────────────────────────────────

    public void RegisterNetworkBridge(IBoardNetworkBridge bridge) 
        => _controller.RegisterBridge(bridge);

    // ── Authoritative sync ───────────────────────────────────────────────

    public void OnAuthoritativeStateReceived(BoardStateData data) 
        => _controller.OnAuthoritativeStateReceived(data);

    // ── Observable handlers ──────────────────────────────────────────────

    private void HandleGridChanged() => GridChanged?.Invoke(_model.GridOccupancy.Value);
}
