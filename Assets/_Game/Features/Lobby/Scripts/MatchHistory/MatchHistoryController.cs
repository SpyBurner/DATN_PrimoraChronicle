using Zenject;

internal class MatchHistoryController : IMatchHistoryController
{
    [Inject] private readonly IMatchHistoryModel _model;

    public void Initialize() { }
    public void Dispose() { }
}
