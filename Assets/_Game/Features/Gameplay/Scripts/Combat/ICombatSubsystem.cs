using UnityEngine.Events;

public interface ICombatSubsystem : ISubsystem
{
    event UnityAction<string> AttackerChanged;
    event UnityAction<string> DefenderChanged;
    event UnityAction<string> LogChanged;

    // Intent
    void ExecuteTurn();
    void SkipCombat();

    // Network registration
    void RegisterNetworkBridge(ICombatNetworkBridge bridge);

    // Authoritative sync
    void OnAuthoritativeStateReceived(CombatStateData data);
}

