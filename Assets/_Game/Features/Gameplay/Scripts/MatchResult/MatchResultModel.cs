using UnityObservables;

internal class MatchResultModel : IMatchResultModel
{
    private readonly Observable<bool> _hasResult = new(false);
    private readonly Observable<GameMatchResult> _result = new(default);

    public Observable<bool> HasResult => _hasResult;
    public Observable<GameMatchResult> Result => _result;

    public void Initialize() { }

    public void Dispose()
    {
        _hasResult.Value = false;
        _result.Value = default;
    }

    public void ApplyState(GameMatchResult data)
    {
        _result.Value = data;
        _hasResult.Value = true;
    }
}
