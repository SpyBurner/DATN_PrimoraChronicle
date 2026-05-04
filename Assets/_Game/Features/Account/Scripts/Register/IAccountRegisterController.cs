using System.Threading.Tasks;

public interface IAccountRegisterController : IController
{
    Task Register(string email, string password, string confirmPassword);
}
