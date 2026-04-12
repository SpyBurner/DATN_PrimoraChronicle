using System.Threading.Tasks;
using Zenject;

public interface IAccountRegisterController : IInitializable
{
    Task Register(string email, string password, string confirmPassword);
    void NavigateToLogin();
}
