using Fusion;
using UnityEngine.Events;

public interface IGameStateSubsystem : ISubsystem
{
    event UnityAction<GameplayPhase> PhaseChanged;
    event UnityAction<float> PhaseTimeRemainingChanged;
    event UnityAction<float> MatchElapsedChanged;
    event UnityAction<int> RoundNumberChanged;
    event UnityAction<PlayerRef> CurrentCombatActorChanged;

    GameplayPhase Phase { get; }
    float PhaseTimeRemaining { get; }
    float MatchElapsed { get; }
    int RoundNumber { get; }
    PlayerRef CurrentCombatActor { get; }

    void RegisterNetworkBridge(IGameStateNetworkBridge bridge);
    void OnAuthoritativeStateReceived(GameStateData data);
}
