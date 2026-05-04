using UnityObservables;

public interface IAuthSessionModel : IModel
{
    Observable<string> CurrentUserId { get; }
    Observable<string> AuthToken { get; }
    Observable<bool> IsLoggedIn { get; }
}
