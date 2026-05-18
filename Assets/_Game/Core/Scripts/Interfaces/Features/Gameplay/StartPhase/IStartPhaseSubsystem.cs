using System;
using System.Collections.Generic;
using UnityEngine.Events;

public interface IStartPhaseSubsystem : ISubsystem
{
    event UnityAction<List<int>> SelectedChampionsChanged;
    event UnityAction<bool> IsReadyChanged;
    event UnityAction<string> StatusChanged;

    // Intent
    void SetIsReady(bool ready);
    void AddChampion(int championId);
    void RemoveChampion(int championId);

    // Network registration
    void RegisterNetworkBridge(IStartPhaseNetworkBridge bridge);

    // Authoritative sync
    void OnAuthoritativeStateReceived(StartPhaseStateData data);
}
