using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine.Events;
using Zenject;

public class PlayerCardZoneSubsystem : IPlayerCardZoneSubsystem
{
    [Inject] private readonly IPlayerCardZoneController _controller;
    [Inject] private readonly IPlayerCardZoneModel _model;

    public event UnityAction<IReadOnlyList<string>> OwnHandChanged;

    public IReadOnlyList<string> GetOwnHand() => _model.GetOwnHand();

    public void Initialize()
    {
        _model.OwnHandChanged += HandleOwnHandChanged;
        _controller.Initialize();
    }

    public void Dispose()
    {
        _model.OwnHandChanged -= HandleOwnHandChanged;
        _controller.Dispose();
        _model.Dispose();
    }

    public void RequestDraw(PlayerRef p, int count) => _controller.RequestDraw(p, count);
    public void RequestKeepCards(PlayerRef p, IReadOnlyList<string> keep) => _controller.RequestKeepCards(p, keep);
    public void RequestPlayMainPhaseSpell(string cardId, HexCoord target) => _controller.RequestPlayMainPhaseSpell(cardId, target);

    public void RegisterNetworkBridge(IPlayerCardZoneNetworkBridge bridge) => _controller.RegisterBridge(bridge);
    public void OnAuthoritativeStateReceived(PlayerCardZonePrivateData data) => _controller.OnAuthoritativeStateReceived(data);

    private void HandleOwnHandChanged(IReadOnlyList<string> hand)
    {
        try { OwnHandChanged?.Invoke(hand); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }
}
