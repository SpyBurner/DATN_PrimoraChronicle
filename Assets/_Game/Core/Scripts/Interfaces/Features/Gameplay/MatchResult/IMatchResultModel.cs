using UnityObservables;

public interface IMatchResultModel : IModel
{
    Observable<bool> HasResult { get; }
    Observable<GameMatchResult> Result { get; }

    void ApplyState(GameMatchResult data);
}
