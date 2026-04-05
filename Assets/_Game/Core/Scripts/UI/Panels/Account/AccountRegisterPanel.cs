using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class AccountRegisterPanel : UIPanel
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField _emailField;
    [SerializeField] private TMP_InputField _passwordField;
    [SerializeField] private TMP_InputField _confirmPasswordField;
    [SerializeField] private Button _registerButton;
    [SerializeField] private Button _closeButton;

    protected virtual void OnEnable()
    {
        _registerButton.onClick.AddListener(HandleRegister);
        _closeButton.onClick.AddListener(HandleBackToLogin);
    }

    protected virtual void OnDisable()
    {
        _registerButton.onClick.RemoveListener(HandleRegister);
        _closeButton.onClick.RemoveListener(HandleBackToLogin);
    }

    private async void HandleRegister()
    {
        string email = _emailField.text.Trim();
        string pass = _passwordField.text;
        string confirmPass = _confirmPasswordField.text;

        if (!ValidateInput(email, pass, confirmPass, out string error))
        {
            Debug.LogError(error);
            return;
        }

        Debug.Log($"Attempting registration for: {email}");
        // TODO: Call auth service here
    }

    private async void HandleBackToLogin()
    {
        await _uiManagerSubsystem.ShowScreen<AccountLoginPanel>();
    }

    public bool ValidateInput(string email, string password, string confirmPassword, out string errorMessage)
    {
        errorMessage = "";

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
        {
            errorMessage = "Fields cannot be empty.";
            return false;
        }

        string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        if (!Regex.IsMatch(email, emailPattern))
        {
            errorMessage = "Invalid email format.";
            return false;
        }

        if (password.Length < 6)
        {
            errorMessage = "Password must be at least 6 characters.";
            return false;
        }

        if (password != confirmPassword)
        {
            errorMessage = "Passwords do not match.";
            return false;
        }

        return true;
    }
}
