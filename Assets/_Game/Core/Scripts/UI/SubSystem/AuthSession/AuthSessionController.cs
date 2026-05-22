using System.Threading.Tasks;
using Zenject;
using UnityEngine; 

public class AuthSessionController : IAuthSessionController
{
    private static readonly string InstanceSuffix = "_" + StableHash(Application.dataPath);
    private static readonly string UserIdKey = "AuthSession_UserId" + InstanceSuffix;
    private static readonly string TokenKey  = "AuthSession_Token"  + InstanceSuffix;

    private static string StableHash(string s)
    {
        unchecked
        {
            uint hash = 2166136261u;
            for (int i = 0; i < s.Length; i++) { hash ^= s[i]; hash *= 16777619u; }
            return hash.ToString("X8");
        }
    }

    [Inject] private readonly IDebugLogger _debugLogger;
    [Inject] private readonly IHttpServiceSubsystem _httpService;
    [Inject] private readonly AuthSessionModel _model;

    public void Initialize()
    {
    }

    public void Dispose()
    {
    }

    public async Task StoreSession(string userId, string authToken)
    {
        _debugLogger.Log($"AuthSession: Storing session for user {userId}");
        
        _model.SetCurrentUserId(userId);
        _model.SetAuthToken(authToken);
        
        _httpService.SetAuthToken(authToken);
        
        PlayerPrefs.SetString(UserIdKey, userId);
        PlayerPrefs.SetString(TokenKey, authToken);
        PlayerPrefs.Save();

        await Task.CompletedTask;
    }

    public async Task ClearSession()
    {
        _debugLogger.Log("AuthSession: Clearing session");
        
        _model.SetCurrentUserId(string.Empty);
        _model.SetAuthToken(string.Empty);
        
        _httpService.SetAuthToken(null);
        
        PlayerPrefs.DeleteKey(UserIdKey);
        PlayerPrefs.DeleteKey(TokenKey);
        PlayerPrefs.Save();

        await Task.CompletedTask;
    }

    public async Task LoadPersistedSession()
    {
        string userId = PlayerPrefs.GetString(UserIdKey, string.Empty);
        string token  = PlayerPrefs.GetString(TokenKey,  string.Empty);

        // One-time migration from legacy (unsuffixed) keys.
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
        {
            string legacyId    = PlayerPrefs.GetString("AuthSession_UserId", string.Empty);
            string legacyToken = PlayerPrefs.GetString("AuthSession_Token",  string.Empty);
            if (!string.IsNullOrEmpty(legacyId) && !string.IsNullOrEmpty(legacyToken))
            {
                _debugLogger.Log($"AuthSession: Migrating legacy session for user {legacyId}");
                userId = legacyId;
                token  = legacyToken;
                PlayerPrefs.SetString(UserIdKey, userId);
                PlayerPrefs.SetString(TokenKey,  token);
                PlayerPrefs.DeleteKey("AuthSession_UserId");
                PlayerPrefs.DeleteKey("AuthSession_Token");
                PlayerPrefs.Save();
            }
        }

        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(token))
        {
            _debugLogger.Log($"AuthSession: Loading persisted session for user {userId}");
            _model.SetCurrentUserId(userId);
            _model.SetAuthToken(token);
            _httpService.SetAuthToken(token);
        }
        else
        {
            _debugLogger.Log("AuthSession: No persisted session found");
        }

        await Task.CompletedTask;
    }
}
