using UnityObservables;

internal class MatchMakingModel : IMatchMakingModel
{
    private Observable<bool> _isSearching = new(false);
    private Observable<string> _status = new(string.Empty);
    private Observable<int> _queuePosition = new(0);

    public Observable<bool> IsSearching { get => _isSearching; }
    public Observable<string> Status { get => _status; }
    public Observable<int> QueuePosition { get => _queuePosition; }
    public Observable<bool> IsMatchFound { get => _isMatchFound; }
    public Observable<int> ConfirmationTimer { get => _confirmationTimer; }

    private Observable<bool> _isMatchFound = new(false);
    private Observable<int> _confirmationTimer = new(0);

    public void Initialize() { }

    public void Dispose()
    {
        _isSearching.Value = false;
        _status.Value = string.Empty;
        _queuePosition.Value = 0;
        _isMatchFound.Value = false;
        _confirmationTimer.Value = 0;
    }

    public void SetIsSearching(bool isSearching) => _isSearching.Value = isSearching;
    public void SetStatus(string status) => _status.Value = status;
    public void SetQueuePosition(int position) => _queuePosition.Value = position;
    public void SetIsMatchFound(bool isFound) => _isMatchFound.Value = isFound;
    public void SetConfirmationTimer(int timer) => _confirmationTimer.Value = timer;
}
