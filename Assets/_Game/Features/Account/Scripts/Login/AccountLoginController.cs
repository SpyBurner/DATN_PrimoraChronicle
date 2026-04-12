using System.Threading.Tasks;
using UnityEngine;
using Zenject;

internal class AccountLoginController : IAccountLoginController
{
    [Inject] private readonly IAccountLoginModel _model;
    [Inject] private readonly ISceneLoaderSubsystem _sceneLoader;
    [Inject] private readonly IUIManagerSubsystem _uiManager;

    public void Initialize() { }

    public async Task Login(string email, string password)
    {
        _model.Email = email;
        _model.Password = password;
        Debug.Log($"Login: {_model.Email}");
        await _sceneLoader.LoadScene("Lobby");
    }

    public void NavigateToRegister()
    {
        _uiManager.CloseView(_uiManager.GetPanel<AccountLoginPanel>());
        _uiManager.ShowScreen<AccountRegisterPanel>();
    }
}
