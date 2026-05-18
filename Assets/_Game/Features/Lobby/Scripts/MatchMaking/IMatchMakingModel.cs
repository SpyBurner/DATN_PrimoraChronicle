using UnityObservables;

public interface IMatchMakingModel : IModel
{
    Observable<string>           Status            { get; }
    Observable<float>            Timer             { get; }
    Observable<int>              PlayerJoinedCount { get; }
    Observable<MatchMakingPhase> Phase             { get; }

    void ApplyState(MatchMakingStateData data);
}
