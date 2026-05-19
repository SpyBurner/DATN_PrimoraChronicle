using System.Threading.Tasks;

public interface IAuthSessionController : IController
{
    Task StoreSession(string userId, string authToken);
    Task ClearSession();
    Task LoadPersistedSession();
}
