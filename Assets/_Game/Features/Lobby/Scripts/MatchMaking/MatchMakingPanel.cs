using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;
using Fusion;

public class MatchMakingPanel : UIPanel
{
    [Inject] private readonly IMatchMakingSubsystem _matchMaking;
    [Inject] private readonly INetworkManagerSubsystem _networkManager;
    [Inject] private readonly IProfileSubsystem _profileSubsystem;

    [Header("Operational UI References")]

    [SerializeField] private Button _cancelButton;
    [SerializeField] private Button _acceptButton;
    [SerializeField] private Button _rejectButton;

    [Header("Display UI References")]
    [SerializeField] private TextMeshProUGUI _statusText;
    [SerializeField] private TextMeshProUGUI _timerText;
    [Space]
    [SerializeField] private GameObject _localPlayerPanel;
    [SerializeField] private GameObject _remotePlayerPanel;
    [Space]
    [SerializeField] private Image _localPlayerAvatar;
    [SerializeField] private TextMeshProUGUI _localPlayerNameText;
    [SerializeField] private TextMeshProUGUI _localPlayerLevelText;
    [Space]
    [SerializeField] private Image _remotePlayerAvatar;
    [SerializeField] private TextMeshProUGUI _remotePlayerNameText;
    [SerializeField] private TextMeshProUGUI _remotePlayerLevelText;

    protected override void OnEnable()
    {
        base.OnEnable();
        _cancelButton?.onClick.AddListener(OnCancel);
        _acceptButton?.onClick.AddListener(OnAccept);
        _rejectButton?.onClick.AddListener(OnReject);

        _matchMaking.StatusChanged += OnStatusChanged;
        _matchMaking.TimerChanged += OnTimerChanged;
        _matchMaking.PhaseChanged += OnPhaseChanged;

        UpdateVisuals(_matchMaking.CurrentPhase);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _cancelButton?.onClick.RemoveListener(OnCancel);
        _acceptButton?.onClick.RemoveListener(OnAccept);
        _rejectButton?.onClick.RemoveListener(OnReject);

        _matchMaking.StatusChanged -= OnStatusChanged;
        _matchMaking.TimerChanged -= OnTimerChanged;
        _matchMaking.PhaseChanged -= OnPhaseChanged;
    }

    private void OnCancel() => _matchMaking.CancelMatchmaking();
    private void OnAccept() => _matchMaking.AcceptMatch();
    private void OnReject() => _matchMaking.RejectMatch();

    private void OnStatusChanged(string status)
    {
        if (_statusText != null) _statusText.text = status;
    }

    private void OnTimerChanged(int timer)
    {
        if (_timerText != null) _timerText.text = timer.ToString();
    }

    private void OnPhaseChanged(MatchMakingPhase phase) => UpdateVisuals(phase);

    private void UpdateVisuals(MatchMakingPhase phase)
    {
        // Player names
        if (_localPlayerNameText != null) _localPlayerNameText.text = _profileSubsystem.Username;
        if (_localPlayerLevelText != null) _localPlayerLevelText.text = _profileSubsystem.Level.ToString();

        bool isIdle        = phase == MatchMakingPhase.Idle || phase == MatchMakingPhase.Failed || phase == MatchMakingPhase.Cancelled;
        bool isSearching   = phase == MatchMakingPhase.Searching;
        bool isMatchFound  = phase == MatchMakingPhase.MatchFound;
        bool isConnecting  = phase == MatchMakingPhase.Connecting;
        bool isConnected   = phase == MatchMakingPhase.Connected;

        if (_cancelButton != null) _cancelButton.gameObject.SetActive(!(isMatchFound || isConnecting || isConnected));
        
        if (_acceptButton != null) _acceptButton.gameObject.SetActive(isMatchFound);
        if (_rejectButton != null) _rejectButton.gameObject.SetActive(isMatchFound);

        if (_remotePlayerPanel != null) _remotePlayerPanel.SetActive(isMatchFound || isConnecting || isConnected);
        
    }
}
