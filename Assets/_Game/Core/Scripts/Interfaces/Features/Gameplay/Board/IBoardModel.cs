using UnityObservables;

public interface IBoardModel : IModel
{
    Observable<bool> IsGenerated { get; }

    void ApplyState(BoardStateData data);
}
