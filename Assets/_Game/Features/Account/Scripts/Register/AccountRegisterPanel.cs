using UnityEngine.UI;
using TMPro;
using Zenject;
using UnityEngine;

public class AccountRegisterPanel : UIPanel
{
    [Inject] private readonly IAccountRegisterSubsystem _accountRegister;

    [SerializeField] private TMP_InputField _emailInput;
    [SerializeField] private TMP_InputField _passwordInput;
    [SerializeField] private TMP_InputField _confirmPasswordInput;
    [SerializeField] private Button _submitButton;
    [SerializeField] private Button _backButton;

    protected override void OnEnable()
    {
        base.OnEnable();
        _submitButton?.onClick.AddListener(OnSubmit);
        _backButton?.onClick.AddListener(OnBack);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _submitButton?.onClick.RemoveListener(OnSubmit);
        _backButton?.onClick.RemoveListener(OnBack);
    }

    private void OnSubmit()
    {
        _accountRegister.Register(_emailInput?.text, _passwordInput?.text, _confirmPasswordInput?.text);
    }

    private void OnBack()
    {
        _accountRegister.NavigateToLogin();
    }
}
