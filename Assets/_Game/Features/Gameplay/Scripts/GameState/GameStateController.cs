using UnityEngine;
using Zenject;

public class GameStateController : IGameStateController
{
    [Inject] private readonly IDebugLogger _debugLogger;
    
    // Inject components from the active hierarchy (GameStateModel is a NetworkBehaviour)
    [Inject] private readonly IGameStateModel _model;

    public void Initialize() { }
    public void Dispose() { }

    public void StartMatch()
    {
        // Network state modifications should only typically happen on state authority (StateAuthority).
        // For simplicity now:
        var netModel = _model as GameStateModel;
        if (netModel != null && netModel.HasStateAuthority)
        {
            netModel.NetworkedTurn = 1;
            netModel.NetworkedPhase = "Init";
            netModel.NetworkedTimer = 60;
            _debugLogger.Log("GameState: Match Started");
        }
    }

    public void EndTurn()
    {
        var netModel = _model as GameStateModel;
        if (netModel != null && netModel.HasStateAuthority)
        {
            netModel.NetworkedTurn++;
            _debugLogger.Log($"GameState: Turn Ended, Now Turn {netModel.NetworkedTurn}");
        }
    }

    public void SetPhase(string phase)
    {
        var netModel = _model as GameStateModel;
        if (netModel != null && netModel.HasStateAuthority)
        {
            netModel.NetworkedPhase = phase;
            _debugLogger.Log($"GameState: Phase changed to {phase}");
        }
    }
}
