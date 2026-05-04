using System;
using System.Threading.Tasks;
using UnityEngine.Events;
using UnityObservables;
using Zenject;

public class AuthSessionSubsystem : IAuthSessionSubsystem, IInitializable, IDisposable
{
    [Inject] private readonly IAuthSessionController _controller;
    [Inject] private readonly IAuthSessionModel _model;

    public event UnityAction<string> CurrentUserIdChanged;
    public event UnityAction<string> AuthTokenChanged;
    public event UnityAction<bool> IsLoggedInChanged;

    public void Initialize()
    {
        if (_model?.CurrentUserId != null)
            _model.CurrentUserId.OnChanged += HandleCurrentUserIdChanged;

        if (_model?.AuthToken != null)
            _model.AuthToken.OnChanged += HandleAuthTokenChanged;

        if (_model?.IsLoggedIn != null)
            _model.IsLoggedIn.OnChanged += HandleIsLoggedInChanged;

        _controller.LoadPersistedSession().ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_model?.CurrentUserId != null)
            _model.CurrentUserId.OnChanged -= HandleCurrentUserIdChanged;

        if (_model?.AuthToken != null)
            _model.AuthToken.OnChanged -= HandleAuthTokenChanged;

        if (_model?.IsLoggedIn != null)
            _model.IsLoggedIn.OnChanged -= HandleIsLoggedInChanged;
    }

    public Task StoreSession(string userId, string authToken) => _controller.StoreSession(userId, authToken);

    public Task ClearSession() => _controller.ClearSession();

    public Task LoadPersistedSession() => _controller.LoadPersistedSession();

    private void HandleCurrentUserIdChanged()
    {
        try
        {
            CurrentUserIdChanged?.Invoke(_model.CurrentUserId.Value);
        }
        catch (Exception)
        {
            // swallow
        }
    }

    private void HandleAuthTokenChanged()
    {
        try
        {
            AuthTokenChanged?.Invoke(_model.AuthToken.Value);
        }
        catch (Exception)
        {
            // swallow
        }
    }

    private void HandleIsLoggedInChanged()
    {
        try
        {
            IsLoggedInChanged?.Invoke(_model.IsLoggedIn.Value);
        }
        catch (Exception)
        {
            // swallow
        }
    }
}
