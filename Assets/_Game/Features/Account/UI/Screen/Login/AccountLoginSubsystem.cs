using System;
using System.Threading.Tasks;
using Zenject;

public class AccountLoginSubsystem : IAccountLoginSubsystem, IInitializable, IDisposable
{
    [Inject] private readonly IAccountLoginController _controller;

    public void Initialize() { }
    public void Dispose() { }

    public Task Login(string email, string password) => _controller.Login(email, password);
    public void NavigateToRegister() => _controller.NavigateToRegister();
}
