using System;
using System.Threading.Tasks;
using UnityEngine.Events;
using UnityObservables;
using Zenject;

public class ProfileSubsystem : IProfileSubsystem, IInitializable, IDisposable
{
    [Inject] private readonly IProfileController _controller;
    [Inject] private readonly IProfileModel _model;

    public event UnityAction<string> UsernameChanged;
    public event UnityAction<int> LevelChanged;
    public event UnityAction<int> XpChanged;
    public event UnityAction<int> XpToNextLevelChanged;
    public event UnityAction<int> GoldChanged;
    public event UnityAction<string> AvatarUrlChanged;

    public void Initialize()
    {
        if (_model?.Username != null)
            _model.Username.OnChanged += HandleUsernameChanged;

        if (_model?.Level != null)
            _model.Level.OnChanged += HandleLevelChanged;

        if (_model?.Xp != null)
            _model.Xp.OnChanged += HandleXpChanged;

        if (_model?.XpToNextLevel != null)
            _model.XpToNextLevel.OnChanged += HandleXpToNextLevelChanged;

        if (_model?.Gold != null)
            _model.Gold.OnChanged += HandleGoldChanged;

        if (_model?.AvatarUrl != null)
            _model.AvatarUrl.OnChanged += HandleAvatarUrlChanged;

        _controller.Initialize().ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_model?.Username != null)
            _model.Username.OnChanged -= HandleUsernameChanged;

        if (_model?.Level != null)
            _model.Level.OnChanged -= HandleLevelChanged;

        if (_model?.Xp != null)
            _model.Xp.OnChanged -= HandleXpChanged;

        if (_model?.XpToNextLevel != null)
            _model.XpToNextLevel.OnChanged -= HandleXpToNextLevelChanged;

        if (_model?.Gold != null)
            _model.Gold.OnChanged -= HandleGoldChanged;

        if (_model?.AvatarUrl != null)
            _model.AvatarUrl.OnChanged -= HandleAvatarUrlChanged;
    }

    private void HandleUsernameChanged()
    {
        try { UsernameChanged?.Invoke(_model.Username.Value); } catch { }
    }

    private void HandleLevelChanged()
    {
        try { LevelChanged?.Invoke(_model.Level.Value); } catch { }
    }

    private void HandleXpChanged()
    {
        try { XpChanged?.Invoke(_model.Xp.Value); } catch { }
    }

    private void HandleXpToNextLevelChanged()
    {
        try { XpToNextLevelChanged?.Invoke(_model.XpToNextLevel.Value); } catch { }
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
