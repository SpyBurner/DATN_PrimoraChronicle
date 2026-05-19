using Fusion;
using UnityObservables;

public interface IGameStateModel : IModel
{
    Observable<GameplayPhase> Phase { get; }
    Observable<float> PhaseTimeRemaining { get; }
    Observable<float> MatchElapsed { get; }
    Observable<int> RoundNumber { get; }
    Observable<PlayerRef> CurrentCombatActor { get; }

    void ApplyState(GameStateData data);
}
