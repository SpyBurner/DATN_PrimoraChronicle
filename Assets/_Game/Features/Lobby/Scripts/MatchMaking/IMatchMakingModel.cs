using UnityObservables;

public interface IMatchMakingModel : IModel
{
    Observable<bool> IsSearching { get; }
    Observable<string> Status { get; }
    Observable<int> QueuePosition { get; }
}
