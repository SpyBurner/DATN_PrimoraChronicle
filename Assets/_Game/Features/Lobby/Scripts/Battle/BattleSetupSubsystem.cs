using System;
using System.Threading.Tasks;
using UnityEngine.Events;
using UnityObservables;
using Zenject;

public class BattleSetupSubsystem : IBattleSetupSubsystem
{
    [Inject] private readonly IBattleSetupController _controller;
    [Inject] private readonly IBattleSetupModel _model;

    public event UnityAction<bool> IsOfflineChanged;
    public event UnityAction<int> BotCountChanged;
    public event UnityAction<int> PlayerCountChanged;
    public event UnityAction<string> ErrorMessageChanged;

    public void Initialize()
    {
        if (_model?.IsOffline != null)
            _model.IsOffline.OnChanged += HandleIsOfflineChanged;

        if (_model?.BotCount != null)
            _model.BotCount.OnChanged += HandleBotCountChanged;

        if (_model?.PlayerCount != null)
            _model.PlayerCount.OnChanged += HandlePlayerCountChanged;

        if (_model?.ErrorMessage != null)
            _model.ErrorMessage.OnChanged += HandleErrorMessageChanged;
    }

    public void Dispose()
    {
        if (_model?.IsOffline != null)
            _model.IsOffline.OnChanged -= HandleIsOfflineChanged;

        if (_model?.BotCount != null)
            _model.BotCount.OnChanged -= HandleBotCountChanged;

        if (_model?.PlayerCount != null)
            _model.PlayerCount.OnChanged -= HandlePlayerCountChanged;

        if (_model?.ErrorMessage != null)
            _model.ErrorMessage.OnChanged -= HandleErrorMessageChanged;
    }

    public void SetOffline(bool isOffline) => _controller.SetOffline(isOffline);
    public void SetBotCount(int count) => _controller.SetBotCount(count);
    public void SetPlayerCount(int count) => _controller.SetPlayerCount(count);
    public Task StartMatchmaking() => _controller.StartMatchmaking();

    private void HandleIsOfflineChanged()
    {
        try { IsOfflineChanged?.Invoke(_model.IsOffline.Value); } catch { }
    }

    private void HandleBotCountChanged()
    {
        try { BotCountChanged?.Invoke(_model.BotCount.Value); } catch { }
    }

    private void HandlePlayerCountChanged()
    {
        try { PlayerCountChanged?.Invoke(_model.PlayerCount.Value); } catch { }
    }

    private void HandleErrorMessageChanged()
    {
        try { ErrorMessageChanged?.Invoke(_model.ErrorMessage.Value); } catch { }
    }
}
