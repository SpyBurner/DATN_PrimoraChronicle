using System.Threading.Tasks;
using Zenject;

public interface IAccountRegisterSubsystem : ISubsystem
{
    Task Register(string email, string password, string confirmPassword);
    void NavigateToLogin();
}
