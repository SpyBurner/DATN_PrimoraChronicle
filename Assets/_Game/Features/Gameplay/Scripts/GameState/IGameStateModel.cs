using UnityObservables;

public interface IGameStateModel : IModel
{
    Observable<int> CurrentTurn { get; }
    Observable<string> CurrentPhase { get; }
    Observable<int> MatchTimer { get; }
}
