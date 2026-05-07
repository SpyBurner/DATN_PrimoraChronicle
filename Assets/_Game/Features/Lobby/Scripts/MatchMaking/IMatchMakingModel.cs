using UnityObservables;

public interface IMatchMakingModel : IModel
{
    Observable<bool> IsSearching { get; }
    Observable<string> Status { get; }
    Observable<int> QueuePosition { get; }

    void SetIsSearching(bool isSearching);
    void SetStatus(string status);
    void SetQueuePosition(int position);
}
