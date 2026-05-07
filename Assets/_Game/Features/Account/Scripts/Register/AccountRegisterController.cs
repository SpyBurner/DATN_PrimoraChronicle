using System;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

internal class AccountRegisterController : IAccountRegisterController
{
    [Inject] private readonly IDebugLogger _debugLogger;
    [Inject] private readonly IAccountRegisterModel _model;
    [Inject] private readonly IHttpServiceSubsystem _httpService;
    [Inject] private readonly IAuthSessionSubsystem _authSession;
    [Inject] private readonly ISceneLoaderSubsystem _sceneLoader;

    public void Initialize() { }
    public void Dispose() { }

    public void SetEmail(string email) => _model.SetEmail(email);
    public void SetPassword(string password) => _model.SetPassword(password);
    public void SetConfirmPassword(string confirmPassword) => _model.SetConfirmPassword(confirmPassword);

    public async Task Register()
    {
        try
        {
            _model.SetErrorMessage(string.Empty);
            _model.SetIsSubmitting(true);

            string username = _model.Email.Value; // We use the email field as username in the BE
            string password = _model.Password.Value;
            string confirmPassword = _model.ConfirmPassword.Value;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
            {
                _model.SetErrorMessage("All fields are required.");
                _model.SetIsSubmitting(false);
                return;
            }

            if (password != confirmPassword)
            {
                _model.SetErrorMessage("Passwords do not match.");
                _model.SetIsSubmitting(false);
                return;
            }

            _debugLogger.Log($"Attempting registration for {username}");

            var payload = new RegisterRequest { username = username, password = password };
            var response = await _httpService.Post<RegisterResponse>("/api/auth/register", payload);

            if (response != null && response.user != null && !string.IsNullOrEmpty(response.token))
            {
                await _authSession.StoreSession(response.user.ID, response.token);
                _debugLogger.Log($"Registration successful for {response.user.username}. Loading Lobby scene.");
                await _sceneLoader.LoadScene("Lobby");
            }
            else
            {
                _model.SetErrorMessage("Registration failed. Email may already be in use.");
            }
        }
        catch (Exception ex)
        {
            _debugLogger.LogError($"Registration failed: {ex.Message}");
            _model.SetErrorMessage($"Registration failed: {ex.Message}");
        }
        finally
        {
            _model.SetIsSubmitting(false);
        }
    }
}

[System.Serializable]
internal class RegisterResponse
{
    public string token;
    public UserDataResponse user;
}
