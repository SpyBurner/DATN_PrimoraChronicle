using Zenject;

public interface ILobbyMainController : IInitializable
{
    void NavigateToProfile();
    void NavigateToBattle();
    void NavigateToDeck();
    void NavigateToShop();
    void NavigateToSettings();
    void Logout();
}
