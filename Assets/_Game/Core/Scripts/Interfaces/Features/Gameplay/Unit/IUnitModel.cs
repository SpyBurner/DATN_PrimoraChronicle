using System;
using System.Collections.Generic;

public interface IUnitModel : IModel
{
    event Action<UnitStateData> UnitStateChanged;
    event Action<string> UnitRemoved;

    IReadOnlyDictionary<string, UnitStateData> Units { get; }

    void ApplyUnitState(UnitStateData data);
    void RemoveUnit(string unitNetworkId);
}
