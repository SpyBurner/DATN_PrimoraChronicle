using System;
using System.Collections.Generic;
using UnityObservables;

public interface ICombatModel : IModel
{
    event Action QueueChanged;

    Observable<string> CurrentActorId { get; }
    Observable<bool> IsCombatActive { get; }
    IReadOnlyList<string> ActionQueue { get; }

    void ApplyState(CombatStateData data);
}
