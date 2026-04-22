using Zenject;

internal class LobbyMainController : ILobbyMainController
{
    [Inject] private readonly ILobbyMainModel _model;
    [Inject] private readonly ISceneLoaderSubsystem _sceneLoader;
    [Inject] private readonly IUIManagerSubsystem _uiManager;

    public void Initialize() { }

    public void NavigateToProfile()
    {
        _uiManager.ShowScreen<ProfilePanel>();
    }

    public void NavigateToBattle()
    {
        _uiManager.ShowScreen<BattlePanel>();
    }

    public void NavigateToDeck()
    {
        _uiManager.ShowScreen<DeckPanel>();
    }

    public void NavigateToShop()
    {
        _uiManager.ShowScreen<ShopPanel>();
    }

    public void NavigateToSettings()
    {
        _uiManager.ShowScreen<SettingPanel>();
    }

    public void Logout()
    {
        _sceneLoader.LoadScene("Account");
    }
}
