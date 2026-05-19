public interface ICombatNetworkBridge
{
    void SendMoveRpc(string unitId, HexCoord destination);
    void SendNormalAttackRpc(string unitId, HexCoord target);
    void SendSkillRpc(string unitId, string skillId, HexCoord target);
    void SendEndTurnRpc();
}
