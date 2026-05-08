using System.Threading.Tasks;

public interface IAccountRegisterController : IController
{
    void SetEmail(string email);
    void SetPassword(string password);
    void SetConfirmPassword(string confirmPassword);
    Task Register();
}
