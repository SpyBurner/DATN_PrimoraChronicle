using UnityEngine;
using UnityEngine.UI;

public class AccountLoginModel
{
    private System.Action _onClose;
    private System.Action _onRegister;

    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _registerButton;

    public void SetOnClose(System.Action onClose)
    {
        _onClose = onClose;
        _closeButton.onClick.AddListener(() => _onClose?.Invoke());
    }
    public void SetOnRegister(System.Action onRegister)
    {
        _onRegister = onRegister;
        _registerButton.onClick.AddListener(() => _onRegister?.Invoke());
    }
}
