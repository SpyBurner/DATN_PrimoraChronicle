using UnityObservables;

public class DrawPhaseModel : IDrawPhaseModel
{
    private Observable<int> _cardsToDraw = new(0);
    public Observable<int> CardsToDraw => _cardsToDraw;

    private Observable<bool> _isDrawing = new(false);
    public Observable<bool> IsDrawing => _isDrawing;

    public void Initialize() { }

    public void Dispose()
    {
        _cardsToDraw.Value = 0;
        _isDrawing.Value = false;
    }

    public void ApplyState(DrawPhaseStateData data)
    {
        _cardsToDraw.Value = data.CardsToDraw;
        _isDrawing.Value = data.IsDrawing;
    }
}
