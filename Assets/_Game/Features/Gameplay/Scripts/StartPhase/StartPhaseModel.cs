using System.Collections.Generic;
using UnityObservables;

public class StartPhaseModel : IStartPhaseModel
{
    private Observable<List<int>> _selectedChampions = new(new List<int>());
    public Observable<List<int>> SelectedChampions => _selectedChampions;

    private Observable<bool> _isReady = new(false);
    public Observable<bool> IsReady => _isReady;

    private Observable<string> _status = new("");
    public Observable<string> Status => _status;

    public void Initialize() { }

    public void Dispose()
    {
        _selectedChampions.Value = new List<int>();
        _isReady.Value = false;
        _status.Value = "";
    }

    public void ApplyState(StartPhaseStateData data)
    {
        _selectedChampions.Value = new List<int>(data.SelectedChampions);
        _isReady.Value = data.IsReady;
        _status.Value = data.Status;
    }
}
