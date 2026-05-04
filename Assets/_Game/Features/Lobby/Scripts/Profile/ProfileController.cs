using System;
using System.Threading.Tasks;
using Zenject;

internal class ProfileController : IProfileController
{
    [Inject] private readonly IDebugLogger _debugLogger;
    [Inject] private readonly IProfileModel _model;
    [Inject] private readonly IHttpServiceSubsystem _httpService;

    public async Task Initialize()
    {
        try
        {
            _debugLogger.Log("Profile: Initializing — fetching profile details");
            // Getting auth user profile. Just mapping to /api/users/me endpoint. Note it requires a ?user_id= query param normally,
            // but we can adjust backend to just use JWT token to deduce user_id, or we inject AuthSession to get UserId here.
            var profile = await _httpService.Get<ProfileDetailResponse>("/api/users/me");

            if (profile != null)
            {
                _model.SetUsername(profile.username);
                _model.SetLevel(profile.level);
                _model.SetXp(profile.xp);
                _model.SetXpToNextLevel(profile.xpToNextLevel);
                _model.SetGold(profile.gold);
                _model.SetAvatarUrl(profile.avatarUrl);
                _debugLogger.Log($"Profile: Loaded details for {profile.username}");
            }
            else
            {
                _debugLogger.LogError("Profile: Failed to load profile details");
            }
        }
        catch (Exception ex)
        {
            _debugLogger.LogError($"Profile: Initialize failed: {ex.Message}");
        }
    }

    public void Dispose() { }
}

[System.Serializable]
internal class ProfileDetailResponse
{
    public string username;
    public int level;
    public int xp;
    public int xpToNextLevel;
    public int gold;
    public string avatarUrl;
}
