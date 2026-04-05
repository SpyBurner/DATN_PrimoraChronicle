using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class AccountLoginPanel : UIPanel
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField _emailField;
    [SerializeField] private TMP_InputField _passwordField;
    [SerializeField] private Button _loginButton;
    [SerializeField] private Button _registerButton;
    // [SerializeField] private TMP_Text _errorText;

    protected virtual void OnEnable()
    {
        _loginButton.onClick.AddListener(HandleLogin);
        _registerButton.onClick.AddListener(HandleRegister);
    }

    protected virtual void OnDisable()
    {
        _loginButton.onClick.RemoveListener(HandleLogin);
        _registerButton.onClick.RemoveListener(HandleRegister);
    }

    private void HandleLogin()
    {
        string email = _emailField.text.Trim();
        string pass = _passwordField.text;

        if (ValidateInput(email, pass, out string error))
        {
            Debug.Log($"Attempting login for: {email}");
            // TODO: Call auth service here
        }
        else
        {
            Debug.LogError(error);
        }
    }

    private async void HandleRegister()
    {
        await _uiManagerSubsystem.ShowScreen<AccountRegisterPanel>();
    }

    // --- Simple Validation Logic ---
    public bool ValidateInput(string email, string password, out string errorMessage)
    {
        errorMessage = "";

        // 1. Check for empty fields
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            errorMessage = "Fields cannot be empty.";
            return false;
        }

        // 2. Simple Email Regex
        string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        if (!Regex.IsMatch(email, emailPattern))
        {
            errorMessage = "Invalid email format.";
            return false;
        }

        // 3. Password Length Check
        if (password.Length < 6)
        {
            errorMessage = "Password must be at least 6 characters.";
            return false;
        }

        return true;
    }
}