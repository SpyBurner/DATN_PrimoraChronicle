using System.Collections.Generic;
using UnityObservables;

public interface IBoardModel : IModel
{
    Observable<Dictionary<int, string>> GridOccupancy { get; }
    void ApplyState(BoardStateData data);
}

