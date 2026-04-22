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
        _emailInput?.onValueChanged.AddListener(OnEmailChanged);
        _passwordInput?.onValueChanged.AddListener(OnPasswordChanged);
        _loginButton?.onClick.AddListener(OnLogin);
        _registerButton?.onClick.AddListener(OnRegister);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _emailInput?.onValueChanged.RemoveListener(OnEmailChanged);
        _passwordInput?.onValueChanged.RemoveListener(OnPasswordChanged);
        _loginButton?.onClick.RemoveListener(OnLogin);
        _registerButton?.onClick.RemoveListener(OnRegister);
    }

    private void OnEmailChanged(string value) => _accountLogin.SetEmail(value);
    private void OnPasswordChanged(string value) => _accountLogin.SetPassword(value);

    private void OnLogin() => _accountLogin.Login();

    private void OnRegister() => _accountLogin.NavigateToRegister();
}
