using UnityEngine;

public interface IBehaviorRegistrySubsystem : ISubsystem
{
    bool TryGetSkillBehavior(string behaviorId, out ScriptableObject behavior);
    bool TryGetStatusEffectBehavior(string behaviorId, out ScriptableObject behavior);
    bool TryGetMainPhaseSpellBehavior(string behaviorId, out ScriptableObject behavior);
}
