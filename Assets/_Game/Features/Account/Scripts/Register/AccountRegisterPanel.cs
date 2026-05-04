using UnityEngine.UI;
using TMPro;
using Zenject;
using UnityEngine;

public class AccountRegisterPanel : UIPanel
{
    [Inject] private readonly IAccountRegisterSubsystem _accountRegister;
    [Inject] private readonly IUIManagerSubsystem _uiManager;

    [SerializeField] private TMP_InputField _emailInput;
    [SerializeField] private TMP_InputField _passwordInput;
    [SerializeField] private TMP_InputField _confirmPasswordInput;
    [SerializeField] private Button _submitButton;
    [SerializeField] private Button _backButton;
    [SerializeField] private TextMeshProUGUI _errorText;

    protected override void OnEnable()
    {
        base.OnEnable();
        _emailInput?.onValueChanged.AddListener(OnEmailChanged);
        _passwordInput?.onValueChanged.AddListener(OnPasswordChanged);
        _confirmPasswordInput?.onValueChanged.AddListener(OnConfirmPasswordChanged);
        _submitButton?.onClick.AddListener(OnSubmit);
        _backButton?.onClick.AddListener(OnBack);
        
        _accountRegister.ErrorMessageChanged += OnErrorMessageChanged;
        _accountRegister.IsSubmittingChanged += OnIsSubmittingChanged;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _emailInput?.onValueChanged.RemoveListener(OnEmailChanged);
        _passwordInput?.onValueChanged.RemoveListener(OnPasswordChanged);
        _confirmPasswordInput?.onValueChanged.RemoveListener(OnConfirmPasswordChanged);
        _submitButton?.onClick.RemoveListener(OnSubmit);
        _backButton?.onClick.RemoveListener(OnBack);
        
        _accountRegister.ErrorMessageChanged -= OnErrorMessageChanged;
        _accountRegister.IsSubmittingChanged -= OnIsSubmittingChanged;
    }

    private void OnEmailChanged(string value) => _accountRegister.SetEmail(value);
    private void OnPasswordChanged(string value) => _accountRegister.SetPassword(value);
    private void OnConfirmPasswordChanged(string value) => _accountRegister.SetConfirmPassword(value);

    private void OnSubmit() => _accountRegister.Register();

    private void OnBack()
    {
        _uiManager.ShowScreen<AccountLoginPanel>();
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
        if (_submitButton != null)
        {
            _submitButton.interactable = !isSubmitting;
        }
    }
}
