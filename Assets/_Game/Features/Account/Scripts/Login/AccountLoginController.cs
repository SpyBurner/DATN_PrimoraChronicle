using System;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

internal class AccountLoginController : IAccountLoginController
{
    [Inject] private readonly IDebugLogger _debugLogger;
    [Inject] private readonly IAccountLoginModel _model;
    [Inject] private readonly IHttpServiceSubsystem _httpService;
    [Inject] private readonly IAuthSessionSubsystem _authSession;
    [Inject] private readonly ISceneLoaderSubsystem _sceneLoader;

    public void Initialize() { }
    public void Dispose() { }

    public void SetEmail(string email) => _model.SetEmail(email);
    public void SetPassword(string password) => _model.SetPassword(password);

    public async Task Login()
    {
        try
        {
            _model.SetErrorMessage(string.Empty);
            _model.SetIsSubmitting(true);

            string username = _model.Email.Value; // We use the email field as username in the BE
            string password = _model.Password.Value;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                _model.SetErrorMessage("Username and password are required.");
                _model.SetIsSubmitting(false);
                return;
            }

            _debugLogger.Log($"Attempting login for {username}");

            var payload = new LoginRequest { username = username, password = password };
            var response = await _httpService.Post<LoginResponse>("/api/auth/login", payload);

            if (response != null && response.user != null && !string.IsNullOrEmpty(response.token))
            {
                await _authSession.StoreSession(response.user.ID, response.token);
                _debugLogger.Log($"Login successful for {response.user.username}. Loading Lobby scene.");
                await _sceneLoader.LoadScene("Lobby");
            }
            else
            {
                _model.SetErrorMessage("Invalid email or password.");
            }
        }
        catch (Exception ex)
        {
            _debugLogger.LogError($"Login failed: {ex.Message}");
            _model.SetErrorMessage($"Login failed: {ex.Message}");
        }
        finally
        {
            _model.SetIsSubmitting(false);
        }
    }
}

[System.Serializable]
internal class LoginResponse
{
    public string token;
    public UserDataResponse user;
}
