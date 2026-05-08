using UnityObservables;

public interface IMatchMakingModel : IModel
{
    Observable<bool> IsSearching { get; }
    Observable<string> Status { get; }
    Observable<int> QueuePosition { get; }
    Observable<bool> IsMatchFound { get; }
    Observable<int> ConfirmationTimer { get; }

    void SetIsSearching(bool isSearching);
    void SetStatus(string status);
    void SetQueuePosition(int position);
    void SetIsMatchFound(bool isFound);
    void SetConfirmationTimer(int timer);
}
