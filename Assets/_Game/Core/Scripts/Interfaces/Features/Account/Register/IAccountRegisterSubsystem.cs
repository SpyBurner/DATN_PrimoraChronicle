using System.Threading.Tasks;
using UnityEngine.Events;

public interface IAccountRegisterSubsystem : ISubsystem
{
    event UnityAction<string> ErrorMessageChanged;
    event UnityAction<bool> IsSubmittingChanged;

    void SetEmail(string email);
    void SetPassword(string password);
    void SetConfirmPassword(string confirmPassword);
    Task Register();
}
