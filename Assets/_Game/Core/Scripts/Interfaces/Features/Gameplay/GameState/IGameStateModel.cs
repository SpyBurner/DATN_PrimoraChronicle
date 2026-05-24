using System;
using Fusion;
using UnityObservables;

public interface IGameStateModel : IModel
{
    Observable<GameplayPhase> Phase { get; }
    Observable<float> PhaseTimeRemaining { get; }
    Observable<float> MatchElapsed { get; }
    Observable<int> RoundNumber { get; }
    Observable<PlayerRef> CurrentCombatActor { get; }

    // playerId is 1-based (PlayerRef.PlayerId)
    bool IsPlayerReady(int playerId);
    void SetPlayerReady(int playerId, bool ready);

    void ApplyState(GameStateData data);
}
