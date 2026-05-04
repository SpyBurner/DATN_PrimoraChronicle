using UnityObservables;

public interface IAccountRegisterModel : IModel
{
    Observable<string> Email { get; }
    Observable<string> Password { get; }
    Observable<string> ConfirmPassword { get; }
    Observable<string> ErrorMessage { get; }
    Observable<bool> IsSubmitting { get; }
}
