using System.Threading.Tasks;
using UnityEngine.Events;

public interface IAuthSessionSubsystem : ISubsystem
{
    event UnityAction<string> CurrentUserIdChanged;
    event UnityAction<string> AuthTokenChanged;
    event UnityAction<bool> IsLoggedInChanged;

    Task StoreSession(string userId, string authToken);
    Task ClearSession();
    Task LoadPersistedSession();
}
