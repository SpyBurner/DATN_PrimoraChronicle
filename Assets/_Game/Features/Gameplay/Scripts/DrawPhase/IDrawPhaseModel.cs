using UnityObservables;

public interface IDrawPhaseModel : IModel
{
    Observable<int> CardsToDraw { get; }
    Observable<bool> IsDrawing { get; }
    void ApplyState(DrawPhaseStateData data);
}

