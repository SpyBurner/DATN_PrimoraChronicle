using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;

public class AccountRegisterPanel : UIPanel
{
    [Inject] private readonly ISceneLoaderSubsystem _sceneLoader;

    [SerializeField] private TMP_InputField _emailInput;
    [SerializeField] private TMP_InputField _passwordInput;
    [SerializeField] private TMP_InputField _confirmPasswordInput;
    [SerializeField] private Button _submitButton;
    [SerializeField] private Button _backButton;

    private AccountRegisterModel _model;

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
        _model = new AccountRegisterModel
        {
            Email = _emailInput?.text,
            Password = _passwordInput?.text,
            ConfirmPassword = _confirmPasswordInput?.text,
        };

        if (_model.Password != _model.ConfirmPassword)
        {
            Debug.LogWarning("Passwords do not match.");
            return;
        }

        Debug.Log($"Register: {_model.Email}");
        _sceneLoader.LoadScene("Lobby");
    }

    private void OnBack()
    {
        _uiManagerSubsystem.ShowScreen<AccountLoginPanel>();
    }
}
