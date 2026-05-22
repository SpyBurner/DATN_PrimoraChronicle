using System;
using System.Collections.Generic;
using Fusion;

public interface IUnitModel : IModel
{
    event Action<UnitPublicData> UnitPublicStateChanged;
    event Action<UnitPrivateData> UnitPrivateStateChanged;
    event Action<NetworkId> UnitRemoved;

    IReadOnlyDictionary<NetworkId, UnitPublicData> Units { get; }

    void ApplyPublicState(UnitPublicData data);
    void ApplyPrivateState(UnitPrivateData data);
    void RemoveUnit(NetworkId unitId);
}
