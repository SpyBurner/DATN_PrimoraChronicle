using System.Collections.Generic;
using UnityObservables;

public interface IBoardModel : IModel
{
    // Simplified grid: cell index to unit ID
    ObservableDictionary<int, string> GridOccupancy { get; }
}
