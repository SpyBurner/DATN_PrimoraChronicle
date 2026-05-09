using System;
using UnityEngine.Events;
using Zenject;

public class DrawPhaseSubsystem : IDrawPhaseSubsystem
{
    private readonly IDrawPhaseController _controller;
    private readonly IDrawPhaseModel _model;

    public event UnityAction<int> CardsToDrawChanged;
    public event UnityAction<bool> IsDrawingChanged;

    public DrawPhaseSubsystem(IDrawPhaseController controller, IDrawPhaseModel model)
    {
        _controller = controller;
        _model = model;
    }

    public void Initialize()
    {
        _model.CardsToDraw.OnChanged += HandleCardsToDrawChanged;
        _model.IsDrawing.OnChanged += HandleIsDrawingChanged;
    }

    public void Dispose()
    {
        _model.CardsToDraw.OnChanged -= HandleCardsToDrawChanged;
        _model.IsDrawing.OnChanged -= HandleIsDrawingChanged;
        
        _controller.Dispose();
        _model.Dispose();
    }

    // ── Intent ──────────────────────────────────────────────────────────

    public void StartDraw(int count) => _controller.StartDraw(count);
    public void CompleteDraw() => _controller.CompleteDraw();

    // ── Network registration ─────────────────────────────────────────────

    public void RegisterNetworkBridge(IDrawPhaseNetworkBridge bridge) 
        => _controller.RegisterBridge(bridge);

    // ── Authoritative sync ───────────────────────────────────────────────

    public void OnAuthoritativeStateReceived(DrawPhaseStateData data) 
        => _controller.OnAuthoritativeStateReceived(data);

    // ── Observable handlers ──────────────────────────────────────────────

    private void HandleCardsToDrawChanged() => CardsToDrawChanged?.Invoke(_model.CardsToDraw.Value);
    private void HandleIsDrawingChanged() => IsDrawingChanged?.Invoke(_model.IsDrawing.Value);
}
