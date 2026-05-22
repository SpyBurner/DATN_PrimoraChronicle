using System;
using UnityEngine.Events;
using Zenject;

public class MatchRewardsSubsystem : IMatchRewardsSubsystem
{
    [Inject] private readonly IMatchRewardsController _controller;
    [Inject] private readonly IMatchRewardsModel _model;

    public event UnityAction<int, int> OwnRewardsReceived;

    public int OwnGold => _model.OwnGold;
    public int OwnXP => _model.OwnXP;

    public void Initialize()
    {
        _model.OwnRewardsReceived += HandleOwnRewardsReceived;
        _controller.Initialize();
    }

    public void Dispose()
    {
        _model.OwnRewardsReceived -= HandleOwnRewardsReceived;
        _controller.Dispose();
        _model.Dispose();
    }

    public void RegisterNetworkBridge(IMatchRewardsPrivateNetworkBridge bridge) => _controller.RegisterBridge(bridge);
    public void OnAuthoritativeStateReceived(MatchRewardsPrivateData data) => _controller.OnAuthoritativeStateReceived(data);

    private void HandleOwnRewardsReceived(int gold, int xp)
    {
        try { OwnRewardsReceived?.Invoke(gold, xp); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }
}
