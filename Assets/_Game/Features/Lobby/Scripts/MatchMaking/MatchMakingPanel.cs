using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;

public class MatchMakingPanel : UIPanel
{
    [Inject] private readonly IMatchMakingSubsystem _matchMaking;
    [Inject] private readonly IUIManagerSubsystem _uiManager;

    [Header("Searching State")]
    [SerializeField] private GameObject _searchingRoot;
    [SerializeField] private TextMeshProUGUI _statusText;
    [SerializeField] private TextMeshProUGUI _queuePositionText;
    [SerializeField] private Button _cancelButton;

    [Header("Match Found State")]
    [SerializeField] private GameObject _matchFoundRoot;
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private Button _acceptButton;
    [SerializeField] private Button _rejectButton;

    protected override void OnEnable()
    {
        base.OnEnable();
        _cancelButton?.onClick.AddListener(OnCancel);
        _acceptButton?.onClick.AddListener(OnAccept);
        _rejectButton?.onClick.AddListener(OnReject);

        _matchMaking.IsSearchingChanged += OnIsSearchingChanged;
        _matchMaking.StatusChanged += OnStatusChanged;
        _matchMaking.QueuePositionChanged += OnQueuePositionChanged;
        _matchMaking.IsMatchFoundChanged += OnIsMatchFoundChanged;
        _matchMaking.ConfirmationTimerChanged += OnConfirmationTimerChanged;

        UpdateVisuals();
        OnStatusChanged(_matchMaking.Status);
        OnQueuePositionChanged(_matchMaking.QueuePosition);
        OnConfirmationTimerChanged(_matchMaking.ConfirmationTimer);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _cancelButton?.onClick.RemoveListener(OnCancel);
        _acceptButton?.onClick.RemoveListener(OnAccept);
        _rejectButton?.onClick.RemoveListener(OnReject);

        _matchMaking.IsSearchingChanged -= OnIsSearchingChanged;
        _matchMaking.StatusChanged -= OnStatusChanged;
        _matchMaking.QueuePositionChanged -= OnQueuePositionChanged;
        _matchMaking.IsMatchFoundChanged -= OnIsMatchFoundChanged;
        _matchMaking.ConfirmationTimerChanged -= OnConfirmationTimerChanged;
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

    private void OnQueuePositionChanged(int pos)
    {
        if (_queuePositionText != null) _queuePositionText.text = $"Queue Position: {pos}";
    }

    private void OnConfirmationTimerChanged(int timer)
    {
        if (_timerText != null) _timerText.text = timer.ToString();
    }

    private void UpdateVisuals()
    {
        bool isSearching = _matchMaking.IsSearching;
        bool isMatchFound = _matchMaking.IsMatchFound;

        if (_searchingRoot != null) _searchingRoot.SetActive(isSearching || (!isSearching && !isMatchFound));
        if (_matchFoundRoot != null) _matchFoundRoot.SetActive(isMatchFound);
    }
}
