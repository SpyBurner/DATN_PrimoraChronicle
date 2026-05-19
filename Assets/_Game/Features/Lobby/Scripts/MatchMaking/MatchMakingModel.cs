using UnityObservables;

internal class MatchMakingModel : IMatchMakingModel
{
    private Observable<string> _status = new(string.Empty);
    private Observable<float> _timer = new(0);
    private Observable<int> _playerJoinedCount = new(0);    
    private Observable<MatchMakingPhase> _phase = new(MatchMakingPhase.Idle);

    public Observable<string> Status { get => _status; }
    public Observable<float> Timer { get => _timer; }
    public Observable<int> PlayerJoinedCount { get => _playerJoinedCount; }
    public Observable<MatchMakingPhase> Phase { get => _phase; }

    public void Initialize() { }

    public void Dispose()
    {
        _status.Value = string.Empty;
        _timer.Value = 0;
        _playerJoinedCount.Value = 0;
        _phase.Value = MatchMakingPhase.Idle;
    }

    public void ApplyState(MatchMakingStateData data)
    {
        _status.Value = data.Status;
        _timer.Value = data.Timer;
        _playerJoinedCount.Value = data.PlayerJoinedCount;
        _phase.Value = data.Phase;
    }
}
