using System;
using System.Collections.Generic;
using Fusion;
using UnityObservables;

internal class CombatModel : ICombatModel
{
    private readonly List<CombatQueueEntry> _actionQueue = new();
    private readonly Observable<NetworkId> _currentActor = new(default);
    private readonly Observable<bool> _hasMoved = new(false);
    private readonly Observable<bool> _hasActed = new(false);

    public event Action QueueChanged;
    public Observable<NetworkId> CurrentActor => _currentActor;
    public Observable<bool> HasMoved => _hasMoved;
    public Observable<bool> HasActed => _hasActed;
    public IReadOnlyList<CombatQueueEntry> ActionQueue => _actionQueue;

    public void Initialize() { }

    public void Dispose()
    {
        _actionQueue.Clear();
        _currentActor.Value = default;
        _hasMoved.Value = false;
        _hasActed.Value = false;
    }

    public void ApplyState(CombatStateData data)
    {
        _actionQueue.Clear();
        if (data.ActionQueue != null) _actionQueue.AddRange(data.ActionQueue);
        QueueChanged?.Invoke();

        _currentActor.Value = data.CurrentActor;
        _hasMoved.Value = data.HasMoved;
        _hasActed.Value = data.HasActed;
    }
}
