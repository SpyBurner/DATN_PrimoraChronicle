using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;

public class MatchMakingPanel : UIPanel
{
    [Inject] private readonly IMatchMakingSubsystem _matchMaking;
    [Inject] private readonly IUIManagerSubsystem _uiManager;

    [SerializeField] private Button           _findMatchButton;

    [SerializeField] private TextMeshProUGUI _statusText;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private Button _acceptButton;
    [SerializeField] private Button _rejectButton;

    protected override void OnEnable()
    {
        base.OnEnable();
        _findMatchButton?.onClick.AddListener(OnFindMatch);
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
        _findMatchButton?.onClick.RemoveListener(OnFindMatch);
        _cancelButton?.onClick.RemoveListener(OnCancel);
        _acceptButton?.onClick.RemoveListener(OnAccept);
        _rejectButton?.onClick.RemoveListener(OnReject);

        _matchMaking.StatusChanged -= OnStatusChanged;
        _matchMaking.TimerChanged -= OnTimerChanged;
        _matchMaking.PhaseChanged -= OnPhaseChanged;
    }

    private void OnFindMatch() => _matchMaking.JoinQueue();
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
        bool isIdle        = phase == MatchMakingPhase.Idle || phase == MatchMakingPhase.Failed || phase == MatchMakingPhase.Cancelled;
        bool isSearching   = phase == MatchMakingPhase.Searching;
        bool isMatchFound  = phase == MatchMakingPhase.MatchFound;
        bool isConnecting  = phase == MatchMakingPhase.Connecting;
        bool isConnected   = phase == MatchMakingPhase.Connected;

        if (_findMatchButton != null) _findMatchButton.gameObject.SetActive(isIdle);
        if (_cancelButton != null) _cancelButton.gameObject.SetActive(isSearching || isConnecting || isConnected);
        
        // MatchFound state could show accept/reject dialog. Since currently Accept/Reject is a stub,
        // we can hide it for now or implement if you have the visual elements.
        if (_acceptButton != null) _acceptButton.gameObject.SetActive(isMatchFound);
        if (_rejectButton != null) _rejectButton.gameObject.SetActive(isMatchFound);
    }
}
