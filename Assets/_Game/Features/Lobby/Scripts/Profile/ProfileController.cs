using System;
using System.Threading.Tasks;
using Zenject;

internal class ProfileController : IProfileController
{
    [Inject] private readonly IDebugLogger _debugLogger;
    [Inject] private readonly IProfileModel _model;
    [Inject] private readonly IHttpServiceSubsystem _httpService;
    [Inject] private readonly IAuthSessionModel _authSessionModel;

    public async void Initialize()
    {
        try
        {
            string userId = _authSessionModel.CurrentUserId.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                _debugLogger.LogError("Profile: Cannot load profile details without a current user id");
                return;
            }

            _debugLogger.Log("Profile: Initializing — fetching profile details");
            string encodedUserId = Uri.EscapeDataString(userId);
            var profile = await _httpService.Get<ProfileDetailResponse>($"/api/users/me?user_id={encodedUserId}");

            if (profile != null)
            {
                _model.SetUsername(profile.username);
                _model.SetLevel(Math.Max(1, profile.level));
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
