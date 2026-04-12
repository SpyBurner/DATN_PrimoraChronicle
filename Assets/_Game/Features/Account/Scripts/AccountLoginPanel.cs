using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class AccountLoginPanel : UIPanel
{
    [Inject] private readonly ISceneLoaderSubsystem _sceneLoader;

    [SerializeField] private Button _loginButton;
    [SerializeField] private Button _registerButton;

    protected override void OnEnable()
    {
        base.OnEnable();
        _loginButton?.onClick.AddListener(OnLogin);
        _registerButton?.onClick.AddListener(OnRegister);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _loginButton?.onClick.RemoveListener(OnLogin);
        _registerButton?.onClick.RemoveListener(OnRegister);
    }

    private void OnLogin()
    {
        _sceneLoader.LoadScene("Lobby");
    }

    private void OnRegister()
    {
        _uiManagerSubsystem.CloseView(this);
        _uiManagerSubsystem.ShowScreen<AccountRegisterPanel>();
    }
}
