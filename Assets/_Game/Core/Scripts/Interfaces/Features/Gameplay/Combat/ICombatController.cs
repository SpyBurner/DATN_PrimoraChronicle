public interface ICombatController : IController
{
    void ExecuteTurn();
    void SkipCombat();
    void RegisterBridge(ICombatNetworkBridge bridge);
    void OnAuthoritativeStateReceived(CombatStateData data);
}

