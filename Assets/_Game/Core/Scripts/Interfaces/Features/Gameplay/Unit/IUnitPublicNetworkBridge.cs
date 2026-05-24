using Fusion;

public interface IUnitPublicNetworkBridge
{
    void SendSpawnUnitRpc(NetworkId unitId, PlayerRef owner, HexCoord position, int maxHP, float speed, int deathAnchor, bool isPersistent);
    void SendUnitDiedRpc(NetworkId unitId);
    void SendUnitHPChangedRpc(NetworkId unitId, int newHP);
    void SendUnitMovedRpc(NetworkId unitId, HexCoord destination);
    void SendStatusAppliedRpc(NetworkId unitId, string statusId, int duration);
    void SendStatusRemovedRpc(NetworkId unitId, string statusId);
}
