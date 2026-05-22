using Fusion;
using UnityEngine.Events;

public interface IGameStateSubsystem : ISubsystem
{
    event UnityAction<GameplayPhase> PhaseChanged;
    event UnityAction<float> PhaseTimeRemainingChanged;
    event UnityAction<float> MatchElapsedChanged;
    event UnityAction<int> RoundNumberChanged;
    event UnityAction<PlayerRef> CurrentCombatActorChanged;
    event UnityAction<PlayerRef, bool> PlayerReadyChanged;
    event UnityAction AllPlayersReady;

    GameplayPhase Phase { get; }
    bool IsReady(PlayerRef p);
    bool AcceptsReadyInput { get; }

    void RequestSetLocalReady(bool ready);
    void RegisterNetworkBridge(IGameStateNetworkBridge bridge);
    void OnAuthoritativeStateReceived(GameStateData data);
}
