using System;
using System.Threading.Tasks;
using Zenject;

public class ProfileController : IProfileController
{
    [Inject] private readonly IDebugLogger _debugLogger;
    [Inject] private readonly IProfileModel _model;
    [Inject] private readonly IHttpServiceSubsystem _httpService;
    [Inject] private readonly IAuthSessionSubsystem _authSession;

    public void Initialize()
    {
        _authSession.CurrentUserIdChanged += OnUserIdChanged;

        if (!string.IsNullOrWhiteSpace(_authSession.UserId))
            FetchProfile(_authSession.UserId);
    }

    public void Dispose()
    {
        _authSession.CurrentUserIdChanged -= OnUserIdChanged;
    }

    private void OnUserIdChanged(string userId)
    {
        if (!string.IsNullOrWhiteSpace(userId))
            FetchProfile(userId);
    }

    private async void FetchProfile(string userId)
    {
        try
        {
            _debugLogger.Log("Profile: Fetching profile details");
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
            _debugLogger.LogError($"Profile: FetchProfile failed: {ex.Message}");
        }
    }
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
