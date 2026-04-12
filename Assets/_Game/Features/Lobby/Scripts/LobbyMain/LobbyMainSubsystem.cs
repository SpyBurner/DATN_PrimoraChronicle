using System;
using Zenject;

public class LobbyMainSubsystem : ILobbyMainSubsystem, IInitializable, IDisposable
{
    [Inject] private readonly ILobbyMainController _controller;

    public void Initialize() { }
    public void Dispose() { }

    public void NavigateToProfile() => _controller.NavigateToProfile();
    public void NavigateToBattle() => _controller.NavigateToBattle();
    public void NavigateToDeck() => _controller.NavigateToDeck();
    public void NavigateToShop() => _controller.NavigateToShop();
    public void NavigateToSettings() => _controller.NavigateToSettings();
    public void Logout() => _controller.Logout();
}
