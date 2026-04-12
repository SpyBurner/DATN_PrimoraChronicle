using UnityEngine.UI;
using TMPro;
using Zenject;
using UnityEngine;

public class AccountLoginPanel : UIPanel
{
    [Inject] private readonly IAccountLoginSubsystem _accountLogin;

    [SerializeField] private TMP_InputField _emailInput;
    [SerializeField] private TMP_InputField _passwordInput;
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
        _accountLogin.Login(_emailInput?.text, _passwordInput?.text);
    }

    private void OnRegister()
    {
        _accountLogin.NavigateToRegister();
    }
}
