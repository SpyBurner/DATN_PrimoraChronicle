using System.Collections.Generic;
using UnityEngine.Events;

public interface ICombatSubsystem : ISubsystem
{
    event UnityAction<IReadOnlyList<string>> QueueChanged;
    event UnityAction<string> CurrentTurnChanged;
    event UnityAction TurnEnded;
    event UnityAction CombatPhaseEnded;

    IReadOnlyList<string> ActionQueue { get; }
    string CurrentActorId { get; }
    bool IsCombatActive { get; }

    void RequestMove(string unitId, HexCoord destination);
    void RequestNormalAttack(string unitId, HexCoord target);
    void RequestSkill(string unitId, string skillId, HexCoord target);
    void RequestEndTurn();

    void RegisterNetworkBridge(ICombatNetworkBridge bridge);
    void OnAuthoritativeStateReceived(CombatStateData data);
}
