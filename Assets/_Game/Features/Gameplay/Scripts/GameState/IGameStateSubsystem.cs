using System;
using UnityEngine.Events;

public interface IGameStateSubsystem : ISubsystem
{
    event UnityAction<int> TurnChanged;
    event UnityAction<string> PhaseChanged;
    event UnityAction<int> TimerChanged;

    // Intent
    void StartMatch();
    void EndTurn();
    void SetPhase(string phase);

    // Network registration
    void RegisterNetworkBridge(IGameStateNetworkBridge bridge);

    // Authoritative sync
    void OnAuthoritativeStateReceived(GameStateStateData data);
}

