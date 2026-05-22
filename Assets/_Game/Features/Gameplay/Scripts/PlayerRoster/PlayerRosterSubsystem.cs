using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine.Events;
using Zenject;

public class PlayerRosterSubsystem : IPlayerRosterSubsystem
{
    [Inject] private readonly IPlayerRosterController _controller;
    [Inject] private readonly IPlayerRosterModel _model;

    public event UnityAction<PlayerRef, int> HPChanged;
    public event UnityAction<PlayerRef, string> NameChanged;
    public event UnityAction<PlayerRef, string> UserIdChanged;

    public IReadOnlyList<PlayerRef> AllPlayers => _model.AllPlayers;

    public int GetHP(PlayerRef p) => _model.GetHP(p);
    public string GetName(PlayerRef p) => _model.GetName(p);
    public string GetUserId(PlayerRef p) => _model.GetUserId(p);

    public void Initialize()
    {
        _model.HPChanged += HandleHPChanged;
        _model.NameChanged += HandleNameChanged;
        _model.UserIdChanged += HandleUserIdChanged;
        _controller.Initialize();
    }

    public void Dispose()
    {
        _model.HPChanged -= HandleHPChanged;
        _model.NameChanged -= HandleNameChanged;
        _model.UserIdChanged -= HandleUserIdChanged;
        _controller.Dispose();
        _model.Dispose();
    }

    public void RegisterNetworkBridge(PlayerRef owner, IPlayerRosterNetworkBridge bridge) => _controller.RegisterBridge(owner, bridge);
    public void OnAuthoritativeStateReceived(PlayerRosterPublicData data) => _controller.OnAuthoritativeStateReceived(data);

    private void HandleHPChanged(PlayerRef p, int hp)
    {
        try { HPChanged?.Invoke(p, hp); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }

    private void HandleNameChanged(PlayerRef p, string name)
    {
        try { NameChanged?.Invoke(p, name); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }

    private void HandleUserIdChanged(PlayerRef p, string userId)
    {
        try { UserIdChanged?.Invoke(p, userId); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }
}
