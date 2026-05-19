using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine.Events;
using Zenject;

public class PlayerCardZoneSubsystem : IPlayerCardZoneSubsystem
{
    [Inject] private readonly IPlayerCardZoneController _controller;
    [Inject] private readonly IPlayerCardZoneModel _model;

    public event UnityAction<PlayerRef, IReadOnlyList<string>> HandChanged;
    public event UnityAction<PlayerRef, int> DeckCountChanged;
    public event UnityAction<PlayerRef, int> DiscardCountChanged;
    public event UnityAction<PlayerRef, int> HPChanged;

    public IReadOnlyList<string> GetHand(PlayerRef player) => _model.GetHand(player);
    public int GetDeckCount(PlayerRef player) => _model.GetDeckCount(player);
    public int GetDiscardCount(PlayerRef player) => _model.GetDiscardCount(player);
    public int GetHP(PlayerRef player) => _model.GetHP(player);

    public void Initialize()
    {
        _model.HPChanged += HandleHPChanged;
        _model.HandChanged += HandleHandChanged;
        _model.DeckCountChanged += HandleDeckCountChanged;
        _model.DiscardCountChanged += HandleDiscardCountChanged;
        _controller.Initialize();
    }

    public void Dispose()
    {
        _model.HPChanged -= HandleHPChanged;
        _model.HandChanged -= HandleHandChanged;
        _model.DeckCountChanged -= HandleDeckCountChanged;
        _model.DiscardCountChanged -= HandleDiscardCountChanged;
        _controller.Dispose();
        _model.Dispose();
    }

    public void RequestDraw(PlayerRef player, int count) => _controller.RequestDraw(player, count);
    public void RequestKeepCards(PlayerRef player, IReadOnlyList<string> keep) => _controller.RequestKeepCards(player, keep);
    public void RequestPlayMainPhaseSpell(string cardId, HexCoord target) => _controller.RequestPlayMainPhaseSpell(cardId, target);

    public void RegisterNetworkBridge(IPlayerCardZoneNetworkBridge bridge) => _controller.RegisterBridge(bridge);
    public void OnAuthoritativeStateReceived(PlayerCardZoneData data) => _controller.OnAuthoritativeStateReceived(data);

    private void HandleHPChanged(PlayerRef p, int hp)
    {
        try { HPChanged?.Invoke(p, hp); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }

    private void HandleHandChanged(PlayerRef p, IReadOnlyList<string> hand)
    {
        try { HandChanged?.Invoke(p, hand); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }

    private void HandleDeckCountChanged(PlayerRef p, int count)
    {
        try { DeckCountChanged?.Invoke(p, count); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }

    private void HandleDiscardCountChanged(PlayerRef p, int count)
    {
        try { DiscardCountChanged?.Invoke(p, count); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }
}
