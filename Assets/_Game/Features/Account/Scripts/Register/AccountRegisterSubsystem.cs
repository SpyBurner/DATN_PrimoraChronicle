using System;
using System.Threading.Tasks;
using Zenject;

public class AccountRegisterSubsystem : IAccountRegisterSubsystem, IInitializable, IDisposable
{
    [Inject] private readonly IAccountRegisterController _controller;

    public void Initialize() { }
    public void Dispose() { }

    public Task Register(string email, string password, string confirmPassword) => _controller.Register(email, password, confirmPassword);
    public void NavigateToLogin() => _controller.NavigateToLogin();
}
