using System.Collections.Generic;
using UnityObservables;

public class BoardModel : IBoardModel
{
    private Observable<Dictionary<int, string>> _gridOccupancy = new(new Dictionary<int, string>());
    public Observable<Dictionary<int, string>> GridOccupancy => _gridOccupancy;

    public void Initialize() { }

    public void Dispose()
    {
        _gridOccupancy.Value = new Dictionary<int, string>();
    }

    public void ApplyState(BoardStateData data)
    {
        // Copy dictionary to ensure change detection works if the reference is same
        _gridOccupancy.Value = new Dictionary<int, string>(data.Grid);
    }
}
