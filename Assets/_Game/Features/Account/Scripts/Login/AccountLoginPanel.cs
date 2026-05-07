using UnityEngine.UI;
using TMPro;
using Zenject;
using UnityEngine;
using System.Xml;

public class AccountLoginPanel : UIPanel
{
    [Inject] private readonly IAccountLoginSubsystem _accountLogin;
    [Inject] private readonly IUIManagerSubsystem _uiManager;

    [SerializeField] private TMP_InputField _emailInput;
    [SerializeField] private TMP_InputField _passwordInput;
    [SerializeField] private Button _loginButton;
    [SerializeField] private Button _registerButton;
    [SerializeField] private TextMeshProUGUI _errorText;

    protected override void OnEnable()
    {
        base.OnEnable();
        _emailInput?.onValueChanged.AddListener(OnEmailChanged);
        _passwordInput?.onValueChanged.AddListener(OnPasswordChanged);
        _loginButton?.onClick.AddListener(OnLogin);
        _registerButton?.onClick.AddListener(OnRegister);
        
        _accountLogin.ErrorMessageChanged += OnErrorMessageChanged;
        _accountLogin.IsSubmittingChanged += OnIsSubmittingChanged;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _emailInput?.onValueChanged.RemoveListener(OnEmailChanged);
        _passwordInput?.onValueChanged.RemoveListener(OnPasswordChanged);
        _loginButton?.onClick.RemoveListener(OnLogin);
        _registerButton?.onClick.RemoveListener(OnRegister);
        
        _accountLogin.ErrorMessageChanged -= OnErrorMessageChanged;
        _accountLogin.IsSubmittingChanged -= OnIsSubmittingChanged;
    }

    private void OnEmailChanged(string value) => _accountLogin.SetEmail(value);
    private void OnPasswordChanged(string value) => _accountLogin.SetPassword(value);

    private void OnLogin() => _accountLogin.Login();

    private void OnRegister()
    {
        _uiManager.Show<AccountRegisterPanel>();
    }
    override protected void OnClose()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnErrorMessageChanged(string errorMessage)
    {
        if (_errorText != null)
        {
            _errorText.text = errorMessage;
            _errorText.enabled = !string.IsNullOrEmpty(errorMessage);
        }
    }

    private void OnIsSubmittingChanged(bool isSubmitting)
    {
        if (_loginButton != null)
        {
            _loginButton.interactable = !isSubmitting;
        }
    }
}
