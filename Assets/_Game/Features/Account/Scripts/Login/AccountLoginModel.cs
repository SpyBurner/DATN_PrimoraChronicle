using UnityObservables;

public class AccountLoginModel : IAccountLoginModel
{
    private Observable<string> _email = new(string.Empty);
    private Observable<string> _password = new(string.Empty);
    private Observable<string> _errorMessage = new(string.Empty);
    private Observable<bool> _isSubmitting = new(false);

    public Observable<string> Email { get => _email; }
    public Observable<string> Password { get => _password; }
    public Observable<string> ErrorMessage { get => _errorMessage; }
    public Observable<bool> IsSubmitting { get => _isSubmitting; }

    public void Initialize() { }

    public void Dispose()
    {
        _email.Value = string.Empty;
        _password.Value = string.Empty;
        _errorMessage.Value = string.Empty;
        _isSubmitting.Value = false;
    }

    public void SetEmail(string email) => _email.Value = email;
    public void SetPassword(string password) => _password.Value = password;
    public void SetErrorMessage(string message) => _errorMessage.Value = message;
    public void SetIsSubmitting(bool isSubmitting) => _isSubmitting.Value = isSubmitting;
}
