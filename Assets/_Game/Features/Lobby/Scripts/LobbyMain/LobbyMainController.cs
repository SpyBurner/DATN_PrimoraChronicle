using System;
using System.Threading.Tasks;
using Zenject;

internal class LobbyMainController : ILobbyMainController
{
    [Inject] private readonly IDebugLogger _debugLogger;
    [Inject] private readonly ILobbyMainModel _model;
    [Inject] private readonly ISceneLoaderSubsystem _sceneLoader;
    [Inject] private readonly IHttpServiceSubsystem _httpService;
    [Inject] private readonly IAuthSessionModel _authSessionModel;

    public async void Initialize()
    {
        try
        {
            string userId = _authSessionModel.CurrentUserId.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                _debugLogger.LogError("LOG_LOBBY_MAIN", nameof(LobbyMainController), "LobbyMain: Cannot load user profile without a current user id");
                return;
            }

            _debugLogger.Log("LOG_LOBBY_MAIN", nameof(LobbyMainController), "LobbyMain: Initializing — fetching user profile");
            string encodedUserId = Uri.EscapeDataString(userId);
            var profile = await _httpService.Get<UserProfileResponse>($"/api/users/me?user_id={encodedUserId}");

            if (profile != null)
            {
                _model.SetUsername(profile.username);
                _model.SetLevel(Math.Max(1, profile.level));
                _model.SetGold(profile.gold);
                _model.SetAvatarUrl(profile.avatarUrl);
                _debugLogger.Log("LOG_LOBBY_MAIN", nameof(LobbyMainController), $"LobbyMain: Loaded profile for {profile.username}");
            }
            else
            {
                _debugLogger.LogError("LOG_LOBBY_MAIN", nameof(LobbyMainController), "LobbyMain: Failed to load user profile");
            }
        }
        catch (Exception ex)
        {
            _debugLogger.LogError("LOG_LOBBY_MAIN", nameof(LobbyMainController), $"LobbyMain: Initialize failed: {ex.Message}");
        }
    }

    public void Dispose() { }

    public async Task Logout()
    {
        try
        {
            _debugLogger.Log("LOG_LOBBY_MAIN", nameof(LobbyMainController), "LobbyMain: Logging out");
            await _sceneLoader.LoadScene("Account");
        }
        catch (Exception ex)
        {
            _debugLogger.LogError("LOG_LOBBY_MAIN", nameof(LobbyMainController), $"LobbyMain: Logout failed: {ex.Message}");
        }
    }
}

[System.Serializable]
internal class UserProfileResponse
{
    public string username;
    public int level;
    public int gold;
    public string avatarUrl;
}
