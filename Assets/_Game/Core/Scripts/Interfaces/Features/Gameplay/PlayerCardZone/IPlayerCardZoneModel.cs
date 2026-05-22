using System;
using System.Collections.Generic;

public interface IPlayerCardZoneModel : IModel
{
    event Action<IReadOnlyList<string>> OwnHandChanged;

    IReadOnlyList<string> GetOwnHand();

    void ApplyState(PlayerCardZonePrivateData data);
}
