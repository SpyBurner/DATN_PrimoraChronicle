using System;
using System.Collections.Generic;
using UnityEngine.Events;

public interface IBoardSubsystem : ISubsystem
{
    event UnityAction<Dictionary<int, string>> GridChanged;

    // Intent
    void PlaceUnit(int cellIndex, string unitId);

    // Network registration
    void RegisterNetworkBridge(IBoardNetworkBridge bridge);

    // Authoritative sync
    void OnAuthoritativeStateReceived(BoardStateData data);
}
