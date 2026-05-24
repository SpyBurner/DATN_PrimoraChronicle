using Fusion;
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
        _logger.Log("LOG_COMBAT", nameof(CombatController), $"Bridge {(bridge == null ? "unregistered" : "registered")}.");
    }

    public void OnAuthoritativeStateReceived(CombatStateData data) => _model.ApplyState(data);

    public void RequestMove(NetworkId unit, HexCoord destination) => _bridge?.SendMoveRpc(unit, destination);
    public void RequestNormalAttack(NetworkId unit, HexCoord target) => _bridge?.SendNormalAttackRpc(unit, target);
    public void RequestSkill(NetworkId unit, string skillId, HexCoord target) => _bridge?.SendSkillRpc(unit, skillId, target);
    public void EndTurn() => _bridge?.SendEndTurnRpc();
}
