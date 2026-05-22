using System;
using Fusion;
using UnityObservables;

internal class GameStateModel : IGameStateModel
{
    private readonly Observable<GameplayPhase> _phase = new(GameplayPhase.Setup);
    private readonly Observable<float> _phaseTimeRemaining = new(0f);
    private readonly Observable<float> _matchElapsed = new(0f);
    private readonly Observable<int> _roundNumber = new(0);
    private readonly Observable<PlayerRef> _currentCombatActor = new(PlayerRef.None);
    // index 0 unused; PlayerId is 1-based (slots 1-4)
    private readonly bool[] _playerReady = new bool[5];

    public Observable<GameplayPhase> Phase => _phase;
    public Observable<float> PhaseTimeRemaining => _phaseTimeRemaining;
    public Observable<float> MatchElapsed => _matchElapsed;
    public Observable<int> RoundNumber => _roundNumber;
    public Observable<PlayerRef> CurrentCombatActor => _currentCombatActor;

    public bool IsPlayerReady(int playerId)
        => playerId >= 1 && playerId < _playerReady.Length && _playerReady[playerId];

    public void SetPlayerReady(int playerId, bool ready)
    {
        if (playerId >= 1 && playerId < _playerReady.Length)
            _playerReady[playerId] = ready;
    }

    public void Initialize() { }

    public void Dispose()
    {
        _phase.Value = GameplayPhase.Setup;
        _phaseTimeRemaining.Value = 0f;
        _matchElapsed.Value = 0f;
        _roundNumber.Value = 0;
        _currentCombatActor.Value = PlayerRef.None;
        Array.Clear(_playerReady, 0, _playerReady.Length);
    }

    public void ApplyState(GameStateData data)
    {
        _phase.Value = data.Phase;
        _phaseTimeRemaining.Value = data.PhaseTimeRemaining;
        _matchElapsed.Value = data.MatchElapsed;
        _roundNumber.Value = data.RoundNumber;
        _currentCombatActor.Value = data.CurrentCombatActor;

        if (data.PlayerReady == null) return;
        for (int i = 0; i < data.PlayerReady.Length; i++)
        {
            int playerId = i + 1;
            if (playerId >= _playerReady.Length) break;
            _playerReady[playerId] = data.PlayerReady[i];
        }
    }
}
