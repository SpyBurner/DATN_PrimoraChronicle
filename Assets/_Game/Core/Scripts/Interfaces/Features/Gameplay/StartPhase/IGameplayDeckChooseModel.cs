using UnityObservables;

public interface IGameplayDeckChooseModel : IModel
{
    Observable<bool> IsReady { get; }
    Observable<string> SelectedDeckId { get; }

    void ApplyState(GameplayDeckChooseStateData data);
}
