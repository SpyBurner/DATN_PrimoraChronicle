using System;
using System.Collections.Generic;
using UnityObservables;

internal class CombatModel : ICombatModel
{
    private readonly List<string> _actionQueue = new();
    private readonly Observable<string> _currentActorId = new(string.Empty);
    private readonly Observable<bool> _isCombatActive = new(false);

    public event Action QueueChanged;
    public Observable<string> CurrentActorId => _currentActorId;
    public Observable<bool> IsCombatActive => _isCombatActive;
    public IReadOnlyList<string> ActionQueue => _actionQueue;

    public void Initialize() { }

    public void Dispose()
    {
        _actionQueue.Clear();
        _currentActorId.Value = string.Empty;
        _isCombatActive.Value = false;
    }

    public void ApplyState(CombatStateData data)
    {
        _actionQueue.Clear();
        if (data.ActionQueue != null) _actionQueue.AddRange(data.ActionQueue);
        QueueChanged?.Invoke();

        _currentActorId.Value = data.CurrentActorId ?? string.Empty;
        _isCombatActive.Value = data.IsCombatActive;
    }
}
