using System.Collections.Generic;
using System;
using UnityEngine.Events;

public interface IHandSubsystem : ISubsystem
{
    event UnityAction<List<string>> CardsChanged;

    // Intent
    void PlayCard(string cardId);

    // Network registration
    void RegisterNetworkBridge(IHandNetworkBridge bridge);

    // Authoritative sync
    void OnAuthoritativeStateReceived(HandStateData data);
}
