using System.Collections.Generic;
using UnityEngine;

public interface IBehaviorRegistryModel : IModel
{
    IReadOnlyDictionary<string, ScriptableObject> SkillBehaviors { get; }
    IReadOnlyDictionary<string, ScriptableObject> StatusEffectBehaviors { get; }
    IReadOnlyDictionary<string, ScriptableObject> MainPhaseSpellBehaviors { get; }

    void ApplyBehaviors(
        IReadOnlyDictionary<string, ScriptableObject> skills,
        IReadOnlyDictionary<string, ScriptableObject> effects,
        IReadOnlyDictionary<string, ScriptableObject> spells);
}
