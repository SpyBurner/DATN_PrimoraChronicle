using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;

public class ProfilePanel : UIPanel
{
    [Inject] private readonly IProfileSubsystem _profile;
    [Inject] private readonly IUIManagerSubsystem _uiManager;

    [SerializeField] private Button _matchHistoryButton;
    [SerializeField] private TextMeshProUGUI _usernameText;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private TextMeshProUGUI _xpText;
    [SerializeField] private TextMeshProUGUI _goldText;

    protected override void OnEnable()
    {
        base.OnEnable();
        _matchHistoryButton?.onClick.AddListener(OnMatchHistory);

        _profile.UsernameChanged += OnUsernameChanged;
        _profile.LevelChanged += OnLevelChanged;
        _profile.XpChanged += OnXpChanged;
        _profile.XpToNextLevelChanged += OnXpToNextLevelChanged;
        _profile.GoldChanged += OnGoldChanged;
        _profile.AvatarUrlChanged += OnAvatarUrlChanged;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _matchHistoryButton?.onClick.RemoveListener(OnMatchHistory);

        _profile.UsernameChanged -= OnUsernameChanged;
        _profile.LevelChanged -= OnLevelChanged;
        _profile.XpChanged -= OnXpChanged;
        _profile.XpToNextLevelChanged -= OnXpToNextLevelChanged;
        _profile.GoldChanged -= OnGoldChanged;
        _profile.AvatarUrlChanged -= OnAvatarUrlChanged;
    }

    // D1: Navigation in View directly via UIManager — not through subsystem
    private void OnMatchHistory() => _uiManager.Show<MatchHistoryPanel>();

    private void OnUsernameChanged(string username)
    {
        if (_usernameText != null) _usernameText.text = username;
    }

    private void OnLevelChanged(int level)
    {
        if (_levelText != null) _levelText.text = $"Lvl {level}";
    }

    private void OnXpChanged(int xp)
    {
        if (_xpText != null) _xpText.text = $"XP: {xp}";
    }

    private void OnXpToNextLevelChanged(int xpToNext)
    {
        // Can update XP bar or combined label here
    }

    private void OnGoldChanged(int gold)
    {
        if (_goldText != null) _goldText.text = $"Gold: {gold}";
    }

    private void OnAvatarUrlChanged(string avatarUrl)
    {
        // TODO: async texture load
    }
}
