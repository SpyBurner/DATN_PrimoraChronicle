using System;
using System.Collections.Generic;
using Fusion;

internal class UnitModel : IUnitModel
{
    private readonly Dictionary<NetworkId, UnitPublicData> _publicUnits = new();
    private readonly Dictionary<NetworkId, List<SkillSlot>> _privateSkills = new();

    public event Action<UnitPublicData> UnitPublicStateChanged;
    public event Action<UnitPrivateData> UnitPrivateStateChanged;
    public event Action<NetworkId> UnitRemoved;

    public IReadOnlyDictionary<NetworkId, UnitPublicData> Units => _publicUnits;

    public bool TryGetOwnSkills(NetworkId id, out IReadOnlyList<SkillSlot> skills)
    {
        if (_privateSkills.TryGetValue(id, out var list))
        {
            skills = list;
            return true;
        }
        skills = null;
        return false;
    }

    public void Initialize() { }

    public void Dispose()
    {
        _publicUnits.Clear();
        _privateSkills.Clear();
    }

    public void ApplyPublicState(UnitPublicData data)
    {
        _publicUnits[data.UnitId] = data;
        UnitPublicStateChanged?.Invoke(data);
    }

    public void ApplyPrivateState(UnitPrivateData data)
    {
        _privateSkills[data.UnitId] = data.Skills ?? new List<SkillSlot>();
        UnitPrivateStateChanged?.Invoke(data);
    }

    public void RemoveUnit(NetworkId unitId)
    {
        bool existed = _publicUnits.Remove(unitId);
        _privateSkills.Remove(unitId);
        if (existed)
            UnitRemoved?.Invoke(unitId);
    }
}
