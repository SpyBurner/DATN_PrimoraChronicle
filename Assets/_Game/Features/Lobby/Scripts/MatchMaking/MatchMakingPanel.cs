using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;

public class MatchMakingPanel : UIPanel
{
    [Inject] private readonly IMatchMakingSubsystem _matchMaking;
    [Inject] private readonly IUIManagerSubsystem _uiManager;

    [SerializeField] private Button           _hostButton;
    [SerializeField] private Button           _joinButton;
    [SerializeField] private TMP_InputField   _sessionNameInput;

    [SerializeField] private TextMeshProUGUI _statusText;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private Button _acceptButton;
    [SerializeField] private Button _rejectButton;

    protected override void OnEnable()
    {
        base.OnEnable();
        _hostButton?.onClick.AddListener(OnHost);
        _joinButton?.onClick.AddListener(OnJoin);
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
        _hostButton?.onClick.RemoveListener(OnHost);
        _joinButton?.onClick.RemoveListener(OnJoin);
        _cancelButton?.onClick.RemoveListener(OnCancel);
        _acceptButton?.onClick.RemoveListener(OnAccept);
        _rejectButton?.onClick.RemoveListener(OnReject);

        _matchMaking.StatusChanged -= OnStatusChanged;
        _matchMaking.TimerChanged -= OnTimerChanged;
        _matchMaking.PhaseChanged -= OnPhaseChanged;
    }

    private void OnHost() => _matchMaking.StartAsHost();
    private void OnJoin() => _matchMaking.StartAsClient(_sessionNameInput.text);
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
        bool isIdle        = phase == MatchMakingPhase.Idle;
        bool isConnecting  = phase == MatchMakingPhase.Connecting;
        bool isConnected   = phase == MatchMakingPhase.Connected;

        if (_hostButton != null) _hostButton.gameObject.SetActive(isIdle);
        if (_joinButton != null) _joinButton.gameObject.SetActive(isIdle);
        if (_sessionNameInput != null) _sessionNameInput.gameObject.SetActive(isIdle);
        if (_cancelButton != null) _cancelButton.gameObject.SetActive(isConnecting || isConnected);
        if (_acceptButton != null) _acceptButton.gameObject.SetActive(false);
        if (_rejectButton != null) _rejectButton.gameObject.SetActive(false);
    }
}
