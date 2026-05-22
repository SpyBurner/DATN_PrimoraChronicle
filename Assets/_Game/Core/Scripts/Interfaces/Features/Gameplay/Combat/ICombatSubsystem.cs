using System.Collections.Generic;
using Fusion;
using UnityEngine.Events;

public interface ICombatSubsystem : ISubsystem
{
    event UnityAction<IReadOnlyList<CombatQueueEntry>> QueueChanged;
    event UnityAction<NetworkId> CurrentTurnChanged;
    event UnityAction TurnEnded;

    IReadOnlyList<CombatQueueEntry> ActionQueue { get; }
    NetworkId CurrentActor { get; }
    bool CurrentActorCanMove { get; }
    bool CurrentActorCanAct { get; }

    void RequestMove(NetworkId unit, HexCoord destination);
    void RequestNormalAttack(NetworkId unit, HexCoord target);
    void RequestSkill(NetworkId unit, string skillId, HexCoord target);
    void EndTurn();

    void RegisterNetworkBridge(ICombatNetworkBridge bridge);
    void OnAuthoritativeStateReceived(CombatStateData data);
}
