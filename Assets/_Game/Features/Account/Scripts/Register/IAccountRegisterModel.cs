using Codice.CM.Common;
using UnityObservables;

public interface IAccountRegisterModel : IModel
{
    Observable<string> Email { get; }
    Observable<string> Password { get; }
    Observable<string> ConfirmPassword { get; }
    Observable<string> ErrorMessage { get; }
    Observable<bool> IsSubmitting { get; }

    public void SetEmail(string email);
    public void SetPassword(string password);
    public void SetConfirmPassword(string confirmPassword);
    public void SetErrorMessage(string message);
    public void SetIsSubmitting(bool isSubmitting);
}
