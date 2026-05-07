using Zenject;

public class GameStateController : IGameStateController
{
    [Inject] private readonly IGameStateModel _model;
    [Inject] private readonly IDebugLogger _debugLogger;

    public void Initialize() { }
    public void Dispose() { }

    public void StartMatch()
    {
        _debugLogger.Log("GameStateController: StartMatch");
    }

    public void EndTurn()
    {
        _debugLogger.Log("GameStateController: EndTurn");
    }

    public void SetPhase(string phase)
    {
        _debugLogger.Log($"GameStateController: SetPhase {phase}");
    }
}
