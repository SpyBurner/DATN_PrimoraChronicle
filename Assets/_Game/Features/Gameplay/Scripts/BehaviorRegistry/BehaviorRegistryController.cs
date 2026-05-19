using System.Collections.Generic;
using UnityEngine;
using Zenject;

internal class BehaviorRegistryController : IBehaviorRegistryController
{
    [Inject] private readonly IBehaviorRegistryModel _model;
    [Inject] private readonly IDebugLogger _logger;

    public void Initialize() { }

    public void Dispose() { }

    public bool TryGetSkillBehavior(string behaviorId, out ScriptableObject behavior)
        => _model.SkillBehaviors.TryGetValue(behaviorId, out behavior);

    public bool TryGetStatusEffectBehavior(string behaviorId, out ScriptableObject behavior)
        => _model.StatusEffectBehaviors.TryGetValue(behaviorId, out behavior);

    public bool TryGetMainPhaseSpellBehavior(string behaviorId, out ScriptableObject behavior)
        => _model.MainPhaseSpellBehaviors.TryGetValue(behaviorId, out behavior);
}
