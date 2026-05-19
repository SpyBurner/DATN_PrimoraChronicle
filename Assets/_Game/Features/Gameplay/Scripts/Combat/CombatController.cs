using Zenject;

internal class CombatController : ICombatController
{
    [Inject] private readonly ICombatModel _model;
    [Inject] private readonly IDebugLogger _logger;

    private ICombatNetworkBridge _bridge;

    public void Initialize() { }

    public void Dispose() => _bridge = null;

    public void RegisterBridge(ICombatNetworkBridge bridge)
    {
        _bridge = bridge;
        _logger.Log($"[Combat] Bridge {(bridge == null ? "unregistered" : "registered")}.");
    }

    public void OnAuthoritativeStateReceived(CombatStateData data) => _model.ApplyState(data);

    public void RequestMove(string unitId, HexCoord destination)
    {
        if (_bridge != null) _bridge.SendMoveRpc(unitId, destination);
    }

    public void RequestNormalAttack(string unitId, HexCoord target)
    {
        if (_bridge != null) _bridge.SendNormalAttackRpc(unitId, target);
    }

    public void RequestSkill(string unitId, string skillId, HexCoord target)
    {
        if (_bridge != null) _bridge.SendSkillRpc(unitId, skillId, target);
    }

    public void RequestEndTurn()
    {
        if (_bridge != null) _bridge.SendEndTurnRpc();
    }
}
