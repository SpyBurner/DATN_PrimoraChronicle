using UnityObservables;

public interface IAccountLoginModel : IModel
{
    Observable<string> Email { get; }
    Observable<string> Password { get; }
    Observable<string> ErrorMessage { get; }
    Observable<bool> IsSubmitting { get; }
}
