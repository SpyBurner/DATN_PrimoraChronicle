using UnityObservables;

public interface IMatchResultModel : IModel
{
    Observable<bool> IsVictory { get; }
    Observable<int> GoldEarned { get; }
    Observable<int> RankProgress { get; }
    void ApplyState(MatchResultStateData data);
}

