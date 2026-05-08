using UnityObservables;
using System.Collections.Generic;

public interface IStartPhaseModel : IModel
{
    ObservableList<int> SelectedChampions { get; }
    Observable<bool> IsReady { get; }
    Observable<string> Status { get; }

    void SetIsReady(bool ready);
    void SetStatus(string status);
    void AddChampion(int championId);
    void RemoveChampion(int championId);
}
