using System;
using UnityEngine.Events;

public interface IDrawPhaseSubsystem : ISubsystem
{
    event UnityAction<int> CardsToDrawChanged;
    event UnityAction<bool> IsDrawingChanged;

    // Intent
    void StartDraw(int count);
    void CompleteDraw();

    // Network registration
    void RegisterNetworkBridge(IDrawPhaseNetworkBridge bridge);

    // Authoritative sync
    void OnAuthoritativeStateReceived(DrawPhaseStateData data);
}
