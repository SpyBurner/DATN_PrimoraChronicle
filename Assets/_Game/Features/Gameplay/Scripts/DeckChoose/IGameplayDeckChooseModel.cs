using UnityObservables;

internal interface IGameplayDeckChooseModel
{
    Observable<bool> IsReady { get; }
    Observable<string> SelectedDeckId { get; }

    void Initialize();
    void Dispose();
    void ApplyState(GameplayDeckChooseStateData data);
}
