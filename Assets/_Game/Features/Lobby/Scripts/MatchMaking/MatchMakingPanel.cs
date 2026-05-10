using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;

public class MatchMakingPanel : UIPanel
{
    [Inject] private readonly IMatchMakingSubsystem _matchMaking;
    [Inject] private readonly IUIManagerSubsystem _uiManager;
    [Inject] private readonly INetworkManagerController _networkManager;

    [SerializeField] private TextMeshProUGUI _statusText;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private Button _acceptButton;
    [SerializeField] private Button _rejectButton;

    protected override void OnEnable()
    {
        base.OnEnable();
        _cancelButton?.onClick.AddListener(OnCancel);
        _acceptButton?.onClick.AddListener(OnAccept);
        _rejectButton?.onClick.AddListener(OnReject);

        _matchMaking.StatusChanged += OnStatusChanged;
        _matchMaking.TimerChanged += OnTimerChanged;

        UpdateVisuals();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _cancelButton?.onClick.RemoveListener(OnCancel);
        _acceptButton?.onClick.RemoveListener(OnAccept);
        _rejectButton?.onClick.RemoveListener(OnReject);

        _matchMaking.StatusChanged -= OnStatusChanged;
        _matchMaking.TimerChanged -= OnTimerChanged;
    }

    private void OnCancel() => _matchMaking.CancelMatchmaking();
    private void OnAccept() => _matchMaking.AcceptMatch();
    private void OnReject() => _matchMaking.RejectMatch();

    private void OnIsSearchingChanged(bool isSearching) => UpdateVisuals();
    private void OnIsMatchFoundChanged(bool isFound) => UpdateVisuals();

    private void OnStatusChanged(string status)
    {
        if (_statusText != null) _statusText.text = status;
    }

    private void OnTimerChanged(int timer)
    {
        if (_timerText != null) _timerText.text = timer.ToString();
    }

    private void UpdateVisuals()
    {


    }
}
