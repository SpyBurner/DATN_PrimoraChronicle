using System;
using System.Threading.Tasks;
using UnityEngine.Events;
using UnityObservables;
using Zenject;

public class LobbyMainSubsystem : ILobbyMainSubsystem, IInitializable, IDisposable
{
    [Inject] private readonly ILobbyMainController _controller;
    [Inject] private readonly ILobbyMainModel _model;

    public event UnityAction<string> UsernameChanged;
    public event UnityAction<int> LevelChanged;
    public event UnityAction<int> GoldChanged;
    public event UnityAction<string> AvatarUrlChanged;

    public void Initialize()
    {
        if (_model?.Username != null)
            _model.Username.OnChanged += HandleUsernameChanged;

        if (_model?.Level != null)
            _model.Level.OnChanged += HandleLevelChanged;

        if (_model?.Gold != null)
            _model.Gold.OnChanged += HandleGoldChanged;

        if (_model?.AvatarUrl != null)
            _model.AvatarUrl.OnChanged += HandleAvatarUrlChanged;

        _controller.Initialize();
    }

    public void Dispose()
    {
        if (_model?.Username != null)
            _model.Username.OnChanged -= HandleUsernameChanged;

        if (_model?.Level != null)
            _model.Level.OnChanged -= HandleLevelChanged;

        if (_model?.Gold != null)
            _model.Gold.OnChanged -= HandleGoldChanged;

        if (_model?.AvatarUrl != null)
            _model.AvatarUrl.OnChanged -= HandleAvatarUrlChanged;
    }

    public Task Logout() => _controller.Logout();

    private void HandleUsernameChanged()
    {
        try { UsernameChanged?.Invoke(_model.Username.Value); } catch { }
    }

    private void HandleLevelChanged()
    {
        try { LevelChanged?.Invoke(_model.Level.Value); } catch { }
    }

    private void HandleGoldChanged()
    {
        try { GoldChanged?.Invoke(_model.Gold.Value); } catch { }
    }

    private void HandleAvatarUrlChanged()
    {
        try { AvatarUrlChanged?.Invoke(_model.AvatarUrl.Value); } catch { }
    }
}
