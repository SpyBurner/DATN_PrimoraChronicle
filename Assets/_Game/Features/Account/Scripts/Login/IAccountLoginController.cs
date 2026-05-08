using System.Threading.Tasks;

public interface IAccountLoginController : IController
{
    void SetEmail(string email);
    void SetPassword(string password);
    Task Login();
}
