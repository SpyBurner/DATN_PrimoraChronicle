using System;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class MatchResultPanel : MonoBehaviour
{
    [Inject] private readonly IMatchResultSubsystem _matchResult;
    [Inject] private readonly IGameStateSubsystem _gameState;
    [Inject] private readonly IProfileSubsystem _profile;
    [Inject] private readonly INetworkManagerSubsystem _network;

    [Header("Player Slots")]
    [SerializeField] private Image _player0Crown;
    [SerializeField] private Image _player0PFP;
    [SerializeField] private TMP_Text _player0Name;
    [SerializeField] private Image _player1Crown;
    [SerializeField] private Image _player1PFP;
    [SerializeField] private TMP_Text _player1Name;
    [SerializeField] private Image _player2Crown;
    [SerializeField] private Image _player2PFP;
    [SerializeField] private TMP_Text _player2Name;

    [Header("Rewards")]
    [SerializeField] private TMP_Text _goldValueText;
    [SerializeField] private TMP_Text _xpValueText;
    [SerializeField] private TMP_Text _timeValueText;

    [Header("Actions")]
    [SerializeField] private Button _confirmButton;

    [Header("Slot Containers")]
    [SerializeField] private GameObject _player0Slot;
    [SerializeField] private GameObject _player1Slot;
    [SerializeField] private GameObject _player2Slot;

    private PlayerRef _localPlayer;

    private void Awake()
    {
        if (_player0Crown == null) throw new System.Exception("[MatchResultPanel._player0Crown] Not assigned in Inspector — see wiring-F6.md F6.2");
        if (_player0PFP == null) throw new System.Exception("[MatchResultPanel._player0PFP] Not assigned in Inspector — see wiring-F6.md F6.2");
        if (_player0Name == null) throw new System.Exception("[MatchResultPanel._player0Name] Not assigned in Inspector — see wiring-F6.md F6.2");
        if (_player1Crown == null) throw new System.Exception("[MatchResultPanel._player1Crown] Not assigned in Inspector — see wiring-F6.md F6.2");
        if (_player1PFP == null) throw new System.Exception("[MatchResultPanel._player1PFP] Not assigned in Inspector — see wiring-F6.md F6.2");
        if (_player1Name == null) throw new System.Exception("[MatchResultPanel._player1Name] Not assigned in Inspector — see wiring-F6.md F6.2");
        if (_player2Crown == null) throw new System.Exception("[MatchResultPanel._player2Crown] Not assigned in Inspector — see wiring-F6.md F6.2");
        if (_player2PFP == null) throw new System.Exception("[MatchResultPanel._player2PFP] Not assigned in Inspector — see wiring-F6.md F6.2");
        if (_player2Name == null) throw new System.Exception("[MatchResultPanel._player2Name] Not assigned in Inspector — see wiring-F6.md F6.2");
        if (_goldValueText == null) throw new System.Exception("[MatchResultPanel._goldValueText] Not assigned in Inspector — see wiring-F6.md F6.2");
        if (_xpValueText == null) throw new System.Exception("[MatchResultPanel._xpValueText] Not assigned in Inspector — see wiring-F6.md F6.2");
        if (_timeValueText == null) throw new System.Exception("[MatchResultPanel._timeValueText] Not assigned in Inspector — see wiring-F6.md F6.2");
        if (_confirmButton == null) throw new System.Exception("[MatchResultPanel._confirmButton] Not assigned in Inspector — see wiring-F6.md F6.2");
        if (_player0Slot == null) throw new System.Exception("[MatchResultPanel._player0Slot] Not assigned in Inspector — see wiring-F6.md F6.2");
        if (_player1Slot == null) throw new System.Exception("[MatchResultPanel._player1Slot] Not assigned in Inspector — see wiring-F6.md F6.2");
        if (_player2Slot == null) throw new System.Exception("[MatchResultPanel._player2Slot] Not assigned in Inspector — see wiring-F6.md F6.2");
    }

    private void OnEnable()
    {
        _localPlayer = _network.Runner != null ? _network.Runner.LocalPlayer : default;
        _matchResult.MatchEnded += OnMatchEnded;
        _gameState.PhaseChanged += OnPhaseChanged;
        _confirmButton?.onClick.AddListener(OnConfirmClicked);

        if (_player2Slot != null) _player2Slot.SetActive(false);

        if (_matchResult.HasResult)
            DisplayResult(_matchResult.Result);
    }

    private void OnDisable()
    {
        _matchResult.MatchEnded -= OnMatchEnded;
        _gameState.PhaseChanged -= OnPhaseChanged;
        _confirmButton?.onClick.RemoveListener(OnConfirmClicked);
    }

    private void OnPhaseChanged(GameplayPhase phase)
    {
        try
        {
            gameObject.SetActive(phase == GameplayPhase.GameOver);
        }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private void OnMatchEnded(GameMatchResult result)
    {
        try
        {
            DisplayResult(result);
        }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private void DisplayResult(GameMatchResult result)
    {
        bool localIsWinner = !result.IsTie && result.Winner == _localPlayer;
        bool opponentIsWinner = !result.IsTie && result.Winner != _localPlayer;

        if (_player0Name != null)
            _player0Name.text = _profile.Username ?? "You";

        if (_player1Name != null)
            _player1Name.text = "Opponent";

        if (_player0Crown != null)
            _player0Crown.enabled = localIsWinner;

        if (_player1Crown != null)
            _player1Crown.enabled = opponentIsWinner;

        if (_goldValueText != null)
            _goldValueText.text = result.GoldEarned.ToString();

        if (_xpValueText != null)
            _xpValueText.text = result.XPEarned.ToString();

        if (_timeValueText != null)
            _timeValueText.text = FormatDuration(result.DurationSeconds);

        if (_confirmButton != null)
            _confirmButton.interactable = true;
    }

    private async void OnConfirmClicked()
    {
        if (_confirmButton != null) _confirmButton.interactable = false;
        await _matchResult.ReturnToLobby();
    }

    private static string FormatDuration(float totalSeconds)
    {
        int minutes = Mathf.FloorToInt(totalSeconds / 60f);
        int seconds = Mathf.FloorToInt(totalSeconds % 60f);
        return $"{minutes:00}:{seconds:00}";
    }
}
