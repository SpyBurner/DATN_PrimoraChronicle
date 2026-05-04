using System;
using System.Threading.Tasks;
using Zenject;

internal class LobbyMainController : ILobbyMainController
{
    [Inject] private readonly IDebugLogger _debugLogger;
    [Inject] private readonly ILobbyMainModel _model;
    [Inject] private readonly ISceneLoaderSubsystem _sceneLoader;
    [Inject] private readonly IHttpServiceSubsystem _httpService;

    public async void Initialize()
    {
        try
        {
            _debugLogger.Log("LobbyMain: Initializing — fetching user profile");
            // Adjust to the right endpoint based on FastAPI definition, likely /api/users/me later
            var profile = await _httpService.Get<UserProfileResponse>("/api/users/me");

            if (profile != null)
            {
                _model.SetUsername(profile.username);
                _model.SetLevel(profile.level);
                _model.SetGold(profile.gold);
                _model.SetAvatarUrl(profile.avatarUrl);
                _debugLogger.Log($"LobbyMain: Loaded profile for {profile.username}");
            }
            else
            {
                _debugLogger.LogError("LobbyMain: Failed to load user profile");
            }
        }
        catch (Exception ex)
        {
            _debugLogger.LogError($"LobbyMain: Initialize failed: {ex.Message}");
        }
    }

    public void Dispose() { }

    public async Task Logout()
    {
        try
        {
            _debugLogger.Log("LobbyMain: Logging out");
            await _sceneLoader.LoadScene("Account");
        }
        catch (Exception ex)
        {
            _debugLogger.LogError($"LobbyMain: Logout failed: {ex.Message}");
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
