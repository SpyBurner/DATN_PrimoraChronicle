using System;
using System.Threading.Tasks;
using Zenject;

public class AccountLoginSubsystem : IAccountLoginSubsystem, IInitializable, IDisposable
{
    [Inject] private readonly IAccountLoginController _controller;

    public void Initialize() { }
    public void Dispose() { }

    public void SetEmail(string email) => _controller.SetEmail(email);
    public void SetPassword(string password) => _controller.SetPassword(password);
    public Task Login() => _controller.Login();
    public void NavigateToRegister() => _controller.NavigateToRegister();
}
