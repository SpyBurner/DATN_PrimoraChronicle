using UnityEngine.UI;
using Zenject;
using UnityEngine;

public class LobbyMainPanel : UIPanel
{
    [Inject] private readonly ILobbyMainSubsystem _lobbyMain;

    [SerializeField] private Button _profileButton;
    [SerializeField] private Button _battleButton;
    [SerializeField] private Button _deckButton;
    [SerializeField] private Button _shopButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _logoutButton;

    protected override void OnEnable()
    {
        base.OnEnable();
        _profileButton?.onClick.AddListener(OnProfile);
        _battleButton?.onClick.AddListener(OnBattle);
        _deckButton?.onClick.AddListener(OnDeck);
        _shopButton?.onClick.AddListener(OnShop);
        _settingsButton?.onClick.AddListener(OnSettings);
        _logoutButton?.onClick.AddListener(OnLogout);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _profileButton?.onClick.RemoveListener(OnProfile);
        _battleButton?.onClick.RemoveListener(OnBattle);
        _deckButton?.onClick.RemoveListener(OnDeck);
        _shopButton?.onClick.RemoveListener(OnShop);
        _settingsButton?.onClick.RemoveListener(OnSettings);
        _logoutButton?.onClick.RemoveListener(OnLogout);
    }

    private void OnProfile() => _lobbyMain.NavigateToProfile();
    private void OnBattle() => _lobbyMain.NavigateToBattle();
    private void OnDeck() => _lobbyMain.NavigateToDeck();
    private void OnShop() => _lobbyMain.NavigateToShop();
    private void OnSettings() => _lobbyMain.NavigateToSettings();
    private void OnLogout() => _lobbyMain.Logout();
}
