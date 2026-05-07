using UnityObservables;

public interface IAccountLoginModel : IModel
{
    Observable<string> Email { get; }
    Observable<string> Password { get; }
    Observable<string> ErrorMessage { get; }
    Observable<bool> IsSubmitting { get; }

    public void SetEmail(string email);
    public void SetPassword(string password);
    public void SetErrorMessage(string message);
    public void SetIsSubmitting(bool isSubmitting);
}
