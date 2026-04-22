using System.Threading.Tasks;
using UnityEngine;
using Zenject;

internal class AccountLoginController : IAccountLoginController
{
    [Inject] private readonly IAccountLoginModel _model;
    [Inject] private readonly ISceneLoaderSubsystem _sceneLoader;
    [Inject] private readonly IUIManagerSubsystem _uiManager;

    public void Initialize() { }

    public void SetEmail(string email) => _model.Email = email;
    public void SetPassword(string password) => _model.Password = password;

    public async Task Login()
    {
        Debug.Log($"Login: {_model.Email}");
        await _sceneLoader.LoadScene("Lobby");
    }

    public void NavigateToRegister()
    {
        _uiManager.CloseView(_uiManager.GetPanel<AccountLoginPanel>());
        _uiManager.ShowScreen<AccountRegisterPanel>();
    }
}
