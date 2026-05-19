using System;
using System.Collections.Generic;

internal class UnitModel : IUnitModel
{
    private readonly Dictionary<string, UnitStateData> _units = new();

    public event Action<UnitStateData> UnitStateChanged;
    public event Action<string> UnitRemoved;

    public IReadOnlyDictionary<string, UnitStateData> Units => _units;

    public void Initialize() { }

    public void Dispose() => _units.Clear();

    public void ApplyUnitState(UnitStateData data)
    {
        _units[data.UnitNetworkId] = data;
        UnitStateChanged?.Invoke(data);
    }

    public void RemoveUnit(string unitNetworkId)
    {
        if (_units.Remove(unitNetworkId))
            UnitRemoved?.Invoke(unitNetworkId);
    }
}
