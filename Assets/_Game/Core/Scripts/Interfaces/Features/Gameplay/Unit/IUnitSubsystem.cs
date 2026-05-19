using System.Collections.Generic;
using Fusion;
using UnityEngine.Events;

public interface IUnitSubsystem : ISubsystem
{
    event UnityAction<string> UnitSpawned;
    event UnityAction<string> UnitDied;
    event UnityAction<string, int> UnitHPChanged;
    event UnityAction<string, HexCoord> UnitMoved;
    event UnityAction<string, string, int> StatusApplied;
    event UnityAction<string, string> StatusRemoved;
    event UnityAction<string, int> GrowthStacksChanged;

    IReadOnlyList<string> AllUnitIds { get; }
    bool TryGetUnit(string unitNetworkId, out UnitStateData data);
    IReadOnlyList<string> GetUnitsOwnedBy(PlayerRef owner);

    void RegisterNetworkBridge(IUnitNetworkBridge bridge);
    void OnUnitStateReceived(UnitStateData data);
    void OnUnitDestroyed(string unitNetworkId);
}
