using Fusion;
using UnityObservables;

internal class GameStateModel : IGameStateModel
{
    private readonly Observable<GameplayPhase> _phase = new(GameplayPhase.Setup);
    private readonly Observable<float> _phaseTimeRemaining = new(0f);
    private readonly Observable<float> _matchElapsed = new(0f);
    private readonly Observable<int> _roundNumber = new(0);
    private readonly Observable<PlayerRef> _currentCombatActor = new(PlayerRef.None);

    public Observable<GameplayPhase> Phase => _phase;
    public Observable<float> PhaseTimeRemaining => _phaseTimeRemaining;
    public Observable<float> MatchElapsed => _matchElapsed;
    public Observable<int> RoundNumber => _roundNumber;
    public Observable<PlayerRef> CurrentCombatActor => _currentCombatActor;

    public void Initialize() { }

    public void Dispose()
    {
        _phase.Value = GameplayPhase.Setup;
        _phaseTimeRemaining.Value = 0f;
        _matchElapsed.Value = 0f;
        _roundNumber.Value = 0;
        _currentCombatActor.Value = PlayerRef.None;
    }

    public void ApplyState(GameStateData data)
    {
        _phase.Value = data.Phase;
        _phaseTimeRemaining.Value = data.PhaseTimeRemaining;
        _matchElapsed.Value = data.MatchElapsed;
        _roundNumber.Value = data.RoundNumber;
        _currentCombatActor.Value = data.CurrentCombatActor;
    }
}
