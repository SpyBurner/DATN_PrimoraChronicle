using System.Collections.Generic;
using Fusion;
using UnityEngine.Events;

public interface IUnitSubsystem : ISubsystem
{
    event UnityAction<NetworkId> UnitSpawned;
    event UnityAction<NetworkId> UnitDied;
    event UnityAction<NetworkId, int> UnitHPChanged;
    event UnityAction<NetworkId, HexCoord> UnitMoved;
    event UnityAction<NetworkId, string, int> StatusApplied;
    event UnityAction<NetworkId, string> StatusRemoved;
    // owner-only — fires only on the unit-owner's client
    event UnityAction<NetworkId, IReadOnlyList<SkillSlot>> OwnUnitSkillsChanged;

    IReadOnlyList<NetworkId> AllUnits { get; }
    bool TryGetPublic(NetworkId id, out UnitPublicData data);
    bool TryGetOwnSkills(NetworkId id, out IReadOnlyList<SkillSlot> skills);

    void RegisterNetworkBridge(IUnitPublicNetworkBridge bridge);
    void RegisterPrivateNetworkBridge(IUnitPrivateNetworkBridge bridge);
    void OnUnitPublicStateReceived(UnitPublicData data);
    void OnUnitPrivateStateReceived(UnitPrivateData data);
    void OnUnitDestroyed(NetworkId unitId);
}
