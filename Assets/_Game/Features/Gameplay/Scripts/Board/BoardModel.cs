using UnityObservables;

internal class BoardModel : IBoardModel
{
    private readonly Observable<bool> _isGenerated = new(false);

    public Observable<bool> IsGenerated => _isGenerated;

    public void Initialize() { }

    public void Dispose() => _isGenerated.Value = false;

    public void ApplyState(BoardStateData data) => _isGenerated.Value = data.IsGenerated;
}
