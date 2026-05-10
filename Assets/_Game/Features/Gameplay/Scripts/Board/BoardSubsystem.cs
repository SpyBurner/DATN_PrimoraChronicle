using System;
using System.Collections.Generic;
using UnityEngine.Events;
using Zenject;

public class BoardSubsystem : IBoardSubsystem
{
    [Inject]
    private readonly IBoardController _controller;
    [Inject]
    private readonly IBoardModel _model;

    public event UnityAction<Dictionary<int, string>> GridChanged;

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
