using UnityEngine;

public interface IBehaviorRegistryController : IController
{
    bool TryGetSkillBehavior(string behaviorId, out ScriptableObject behavior);
    bool TryGetStatusEffectBehavior(string behaviorId, out ScriptableObject behavior);
    bool TryGetMainPhaseSpellBehavior(string behaviorId, out ScriptableObject behavior);
}
