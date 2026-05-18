using UnityObservables;

public class GameplayDeckChooseModel : IGameplayDeckChooseModel
{
    private readonly Observable<bool> _isReady = new(false);
    private readonly Observable<string> _selectedDeckId = new(string.Empty);

    public Observable<bool> IsReady => _isReady;
    public Observable<string> SelectedDeckId => _selectedDeckId;

    public void Initialize() { }

    public void Dispose()
    {
        _isReady.Value = false;
        _selectedDeckId.Value = string.Empty;
    }

    public void ApplyState(GameplayDeckChooseStateData data)
    {
        _isReady.Value = data.IsReady;
        _selectedDeckId.Value = data.SelectedDeckId ?? string.Empty;
    }
}
