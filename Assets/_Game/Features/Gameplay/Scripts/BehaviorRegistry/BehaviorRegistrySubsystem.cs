using UnityEngine;
using Zenject;

public class BehaviorRegistrySubsystem : IBehaviorRegistrySubsystem
{
    [Inject] private readonly IBehaviorRegistryController _controller;
    [Inject] private readonly IBehaviorRegistryModel _model;

    public void Initialize() => _controller.Initialize();

    public void Dispose()
    {
        _controller.Dispose();
        _model.Dispose();
    }

    public bool TryGetSkillBehavior(string behaviorId, out ScriptableObject behavior)
        => _controller.TryGetSkillBehavior(behaviorId, out behavior);

    public bool TryGetStatusEffectBehavior(string behaviorId, out ScriptableObject behavior)
        => _controller.TryGetStatusEffectBehavior(behaviorId, out behavior);

    public bool TryGetMainPhaseSpellBehavior(string behaviorId, out ScriptableObject behavior)
        => _controller.TryGetMainPhaseSpellBehavior(behaviorId, out behavior);
}
