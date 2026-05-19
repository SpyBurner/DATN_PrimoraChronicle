using System.Threading.Tasks;
using UnityEngine.Events;

public interface IAccountLoginSubsystem : ISubsystem
{
    event UnityAction<string> ErrorMessageChanged;
    event UnityAction<bool> IsSubmittingChanged;

    void SetEmail(string email);
    void SetPassword(string password);
    Task Login();
}
