using UnityObservables;

public class MatchResultModel : IMatchResultModel
{
    private Observable<bool> _isVictory = new(false);
    public Observable<bool> IsVictory => _isVictory;

    private Observable<int> _goldEarned = new(0);
    public Observable<int> GoldEarned => _goldEarned;

    private Observable<int> _rankProgress = new(0);
    public Observable<int> RankProgress => _rankProgress;

    public void Initialize() { }

    public void Dispose()
    {
        _isVictory.Value = false;
        _goldEarned.Value = 0;
        _rankProgress.Value = 0;
    }

    public void ApplyState(MatchResultStateData data)
    {
        _isVictory.Value = data.IsVictory;
        _goldEarned.Value = data.GoldEarned;
        _rankProgress.Value = data.RankProgress;
    }
}
