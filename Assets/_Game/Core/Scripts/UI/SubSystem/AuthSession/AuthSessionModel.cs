using UnityObservables;

internal class AuthSessionModel : IAuthSessionModel
{
    private Observable<string> _currentUserId = new(string.Empty);
    private Observable<string> _authToken = new(string.Empty);
    private Observable<bool> _isLoggedIn = new(false);

    public Observable<string> CurrentUserId { get => _currentUserId; }
    public Observable<string> AuthToken { get => _authToken; }
    public Observable<bool> IsLoggedIn { get => _isLoggedIn; }

    public void Initialize()
    {
    }

    public void Dispose()
    {
        _currentUserId.Value = string.Empty;
        _authToken.Value = string.Empty;
        _isLoggedIn.Value = false;
    }

    public void SetCurrentUserId(string userId)
    {
        _currentUserId.Value = userId;
        UpdateIsLoggedIn();
    }

    public void SetAuthToken(string token)
    {
        _authToken.Value = token;
        UpdateIsLoggedIn();
    }

    private void UpdateIsLoggedIn()
    {
        _isLoggedIn.Value = !string.IsNullOrEmpty(_currentUserId.Value) && !string.IsNullOrEmpty(_authToken.Value);
    }
}
