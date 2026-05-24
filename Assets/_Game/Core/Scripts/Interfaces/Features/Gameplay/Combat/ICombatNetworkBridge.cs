using Fusion;

public interface ICombatNetworkBridge
{
    void SendMoveRpc(NetworkId unit, HexCoord destination);
    void SendNormalAttackRpc(NetworkId unit, HexCoord target);
    void SendSkillRpc(NetworkId unit, string skillId, HexCoord target);
    void SendEndTurnRpc();
}
