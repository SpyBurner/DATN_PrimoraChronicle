using System.Threading.Tasks;
using Zenject;

public interface IAccountLoginController : IInitializable
{
    Task Login(string email, string password);
    void NavigateToRegister();
}
