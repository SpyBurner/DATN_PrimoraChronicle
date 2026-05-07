using Zenject;

public class MatchResultController : IMatchResultController
{
    [Inject] private readonly IMatchResultModel _model;
    [Inject] private readonly IDebugLogger _debugLogger;

    public void Initialize() { }
    public void Dispose() { }

    public void ShowResult(bool victory, int gold, int rank)
    {
        _debugLogger.Log($"MatchResultController: ShowResult {victory}, {gold}, {rank}");
    }

    public void BackToLobby()
    {
        _debugLogger.Log("MatchResultController: BackToLobby");
    }
}
