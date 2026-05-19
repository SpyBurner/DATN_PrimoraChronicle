using System;
using Fusion;
using TMPro;
using UnityEngine;
using Zenject;

public class GameplayHUDController : MonoBehaviour
{
    [Inject] private readonly IGameStateSubsystem _gameState;
    [Inject] private readonly INetworkManagerSubsystem _network;

    [SerializeField] private TMP_Text _phaseNameText;
    [SerializeField] private TMP_Text _matchTimeText;
    [SerializeField] private GameplayPlayerProfileUI _localProfile;
    [SerializeField] private GameplayPlayerProfileUI _opponentProfile;
    [SerializeField] private GameObject _enemy2ProfileRoot;

    private bool _profilesBound;

    private void OnEnable()
    {
        if (_enemy2ProfileRoot != null) _enemy2ProfileRoot.SetActive(false);

        _gameState.PhaseChanged += OnPhaseChanged;
        _gameState.MatchElapsedChanged += OnMatchElapsedChanged;
        _network.PlayerJoined += OnPlayerJoined;

        OnPhaseChanged(_gameState.Phase);
        OnMatchElapsedChanged(_gameState.MatchElapsed);
        TryBindProfiles();
    }

    private void OnDisable()
    {
        _gameState.PhaseChanged -= OnPhaseChanged;
        _gameState.MatchElapsedChanged -= OnMatchElapsedChanged;
        _network.PlayerJoined -= OnPlayerJoined;
    }

    private void OnPlayerJoined(PlayerRef _) => TryBindProfiles();

    private void TryBindProfiles()
    {
        if (_profilesBound) return;

        var runner = _network.Runner;
        if (runner == null) return;

        PlayerRef local = runner.LocalPlayer;
        PlayerRef opponent = PlayerRef.None;

        foreach (PlayerRef p in runner.ActivePlayers)
        {
            if (p != local) { opponent = p; break; }
        }

        if (local == PlayerRef.None || opponent == PlayerRef.None) return;

        _localProfile?.Bind(local, isLocal: true);
        _opponentProfile?.Bind(opponent, isLocal: false);
        _profilesBound = true;
    }

    private void OnPhaseChanged(GameplayPhase phase)
    {
        try
        {
            if (_phaseNameText != null) _phaseNameText.text = ToDisplayName(phase);
        }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private void OnMatchElapsedChanged(float elapsed)
    {
        try
        {
            if (_matchTimeText == null) return;
            int m = Mathf.FloorToInt(elapsed / 60f);
            int s = Mathf.FloorToInt(elapsed % 60f);
            _matchTimeText.text = $"{m:00}:{s:00}";
        }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private static string ToDisplayName(GameplayPhase phase) => phase switch
    {
        GameplayPhase.Setup       => "SETUP",
        GameplayPhase.StartPhase  => "START PHASE",
        GameplayPhase.MainPhase   => "MAIN PHASE",
        GameplayPhase.CombatPhase => "COMBAT PHASE",
        GameplayPhase.DrawPhase   => "DRAW PHASE",
        GameplayPhase.GameOver    => "GAME OVER",
        _                         => phase.ToString().ToUpper(),
    };
}
