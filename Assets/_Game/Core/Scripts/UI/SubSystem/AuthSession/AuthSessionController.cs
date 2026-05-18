using System.Threading.Tasks;
using Zenject;
using UnityEngine; 

public class AuthSessionController : IAuthSessionController
{
    private const string UserIdKey = "AuthSession_UserId";
    private const string TokenKey = "AuthSession_Token";

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
        string token = PlayerPrefs.GetString(TokenKey, string.Empty);

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
