using System.Threading.Tasks;
using Zenject;

public interface IAccountLoginController : IInitializable
{
    void SetEmail(string email);
    void SetPassword(string password);
    Task Login();
    void NavigateToRegister();
}
