using UnityEngine.UI;
using TMPro;
using Zenject;
using UnityEngine;

public class LobbyMainPanel : UIPanel
{
    [Inject] private readonly ILobbyMainSubsystem _lobbyMain;
    [Inject] private readonly IUIManagerSubsystem _uiManager;

    [SerializeField] private Button _profileButton;
    [SerializeField] private Button _battleButton;
    [SerializeField] private Button _deckButton;
    [SerializeField] private Button _shopButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _logoutButton;

    [SerializeField] private TextMeshProUGUI _usernameText;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private TextMeshProUGUI _goldText;
    [SerializeField] private Image _avatarImage;

    protected override void OnEnable()
    {
        base.OnEnable();
        _profileButton?.onClick.AddListener(OnProfile);
        _battleButton?.onClick.AddListener(OnBattle);
        _deckButton?.onClick.AddListener(OnDeck);
        _shopButton?.onClick.AddListener(OnShop);
        _settingsButton?.onClick.AddListener(OnSettings);
        _logoutButton?.onClick.AddListener(OnLogout);

        _lobbyMain.UsernameChanged += OnUsernameChanged;
        _lobbyMain.LevelChanged += OnLevelChanged;
        _lobbyMain.GoldChanged += OnGoldChanged;
        _lobbyMain.AvatarUrlChanged += OnAvatarUrlChanged;

        _lobbyMain.Refresh();
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

        _lobbyMain.UsernameChanged -= OnUsernameChanged;
        _lobbyMain.LevelChanged -= OnLevelChanged;
        _lobbyMain.GoldChanged -= OnGoldChanged;
        _lobbyMain.AvatarUrlChanged -= OnAvatarUrlChanged;
    }

    private void OnProfile() => _uiManager.Show<ProfilePanel>();
    private void OnBattle() => _uiManager.Show<BattlePanel>();
    private void OnDeck() => _uiManager.Show<DeckPanel>();
    private void OnShop() => _uiManager.Show<ShopPanel>();
    private void OnSettings() => _uiManager.Show<SettingPanel>();
    private void OnLogout() => _lobbyMain.Logout();

    private void OnUsernameChanged(string username)
    {
        if (_usernameText != null)
            _usernameText.text = username;
    }

    private void OnLevelChanged(int level)
    {
        if (_levelText != null)
            _levelText.text = $"Lvl {level}";
    }

    private void OnGoldChanged(int gold)
    {
        if (_goldText != null)
            _goldText.text = $"Gold: {gold}";
    }

    private void OnAvatarUrlChanged(string avatarUrl)
    {
        // Avatar loading would require async texture loading
        // For now, just log that URL changed
        if (!string.IsNullOrEmpty(avatarUrl))
        {
            // TODO: Load texture from URL and apply to _avatarImage
        }
    }
}
