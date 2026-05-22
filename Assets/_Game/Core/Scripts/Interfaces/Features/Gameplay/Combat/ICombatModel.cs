using System;
using System.Collections.Generic;
using Fusion;
using UnityObservables;

public interface ICombatModel : IModel
{
    event Action QueueChanged;

    Observable<NetworkId> CurrentActor { get; }
    Observable<bool> HasMoved { get; }
    Observable<bool> HasActed { get; }
    IReadOnlyList<CombatQueueEntry> ActionQueue { get; }

    void ApplyState(CombatStateData data);
}
