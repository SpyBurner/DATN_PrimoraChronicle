using UnityObservables;

internal class MatchMakingModel : IMatchMakingModel
{
    private Observable<string> _status = new(string.Empty);
    private Observable<float> _timer = new(0);
    private Observable<int> _playerJoinedCount = new(0);    

    public Observable<string> Status { get => _status; }
    public Observable<float> Timer { get => _timer; }
    public Observable<int> PlayerJoinedCount { get => _playerJoinedCount; }

    public void Initialize() { }

    public void Dispose()
    {
        _status.Value = string.Empty;
        _timer.Value = 0;
        _playerJoinedCount.Value = 0;
    }

    public void SetStatus(string status) => _status.Value = status;
    public void SetTimer(float timer) => _timer.Value = timer;
    public void SetPlayerJoinedCount(int count) => _playerJoinedCount.Value = count;
}
