public interface ICombatController : IController
{
    void RequestMove(string unitId, HexCoord destination);
    void RequestNormalAttack(string unitId, HexCoord target);
    void RequestSkill(string unitId, string skillId, HexCoord target);
    void RequestEndTurn();
    void RegisterBridge(ICombatNetworkBridge bridge);
    void OnAuthoritativeStateReceived(CombatStateData data);
}
