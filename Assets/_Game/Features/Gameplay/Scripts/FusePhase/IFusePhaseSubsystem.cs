using System;
using UnityEngine.Events;

public interface IFusePhaseSubsystem : ISubsystem
{
    event UnityAction<bool> IsActiveChanged;
    event UnityAction<string> PrimaryUnitIdChanged;
    event UnityAction<string> SecondaryUnitIdChanged;

    // Intent
    void SetUnits(string primaryId, string secondaryId);
    void Fuse();
    void Cancel();

    // Network registration
    void RegisterNetworkBridge(IFusePhaseNetworkBridge bridge);

    // Authoritative sync
    void OnAuthoritativeStateReceived(FusePhaseStateData data);
}
