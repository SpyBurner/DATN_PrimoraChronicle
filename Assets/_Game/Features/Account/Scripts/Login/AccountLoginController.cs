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

            string email = _model.Email.Value;
            string password = _model.Password.Value;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                _model.SetErrorMessage("Email and password are required.");
                _model.SetIsSubmitting(false);
                return;
            }

            _debugLogger.Log($"Attempting login for {email}");

            var payload = new { email, password };
            var response = await _httpService.Post<LoginResponse>("https://api.example.com/auth/login", payload);

            if (response != null && !string.IsNullOrEmpty(response.token))
            {
                await _authSession.StoreSession(response.userId, response.token);
                _debugLogger.Log($"Login successful. Loading Lobby scene.");
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
    public string userId;
    public string token;
}
