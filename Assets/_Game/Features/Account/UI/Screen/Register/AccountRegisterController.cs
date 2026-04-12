using System.Threading.Tasks;
using UnityEngine;
using Zenject;

internal class AccountRegisterController : IAccountRegisterController
{
    [Inject] private readonly IAccountRegisterModel _model;
    [Inject] private readonly ISceneLoaderSubsystem _sceneLoader;
    [Inject] private readonly IUIManagerSubsystem _uiManager;

    public void Initialize() { }

    public async Task Register(string email, string password, string confirmPassword)
    {
        if (password != confirmPassword)
        {
            Debug.LogWarning("Passwords do not match.");
            return;
        }

        _model.Email = email;
        _model.Password = password;
        _model.ConfirmPassword = confirmPassword;
        Debug.Log($"Register: {_model.Email}");
        await _sceneLoader.LoadScene("Lobby");
    }

    public void NavigateToLogin()
    {
        _uiManager.CloseView(_uiManager.GetPanel<AccountRegisterPanel>());
        _uiManager.ShowScreen<AccountLoginPanel>();
    }
}
