using UnityObservables;
using System.Collections.Generic;

public interface IStartPhaseModel : IModel
{
    Observable<List<int>> SelectedChampions { get; }
    Observable<bool> IsReady { get; }
    Observable<string> Status { get; }

    void ApplyState(StartPhaseStateData data);
}
