using System.Threading.Tasks;
using Zenject;

public interface IAccountLoginSubsystem : ISubsystem
{
    Task Login(string email, string password);
    void NavigateToRegister();
}
