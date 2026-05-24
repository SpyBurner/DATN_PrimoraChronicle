using Fusion;

public interface ICombatController : IController
{
    void RequestMove(NetworkId unit, HexCoord destination);
    void RequestNormalAttack(NetworkId unit, HexCoord target);
    void RequestSkill(NetworkId unit, string skillId, HexCoord target);
    void EndTurn();
    void RegisterBridge(ICombatNetworkBridge bridge);
    void OnAuthoritativeStateReceived(CombatStateData data);
}
