using System;
using System.Collections.Generic;
using UnityEngine.Events;
using Zenject;

public class HandSubsystem : IHandSubsystem
{
    private readonly IHandController _controller;
    private readonly IHandModel _model;

    public event UnityAction<List<string>> CardsChanged;

    public HandSubsystem(IHandController controller, IHandModel model)
    {
        _controller = controller;
        _model = model;
    }

    public void Initialize()
    {
        _model.Cards.OnChanged += HandleCardsChanged;
    }

    public void Dispose()
    {
        _model.Cards.OnChanged -= HandleCardsChanged;
        
        _controller.Dispose();
        _model.Dispose();
    }

    // ── Intent ──────────────────────────────────────────────────────────

    public void PlayCard(string cardId) => _controller.PlayCard(cardId);

    // ── Network registration ─────────────────────────────────────────────

    public void RegisterNetworkBridge(IHandNetworkBridge bridge) 
        => _controller.RegisterBridge(bridge);

    // ── Authoritative sync ───────────────────────────────────────────────

    public void OnAuthoritativeStateReceived(HandStateData data) 
        => _controller.OnAuthoritativeStateReceived(data);

    // ── Observable handlers ──────────────────────────────────────────────

    private void HandleCardsChanged() => CardsChanged?.Invoke(_model.Cards.Value);
}
