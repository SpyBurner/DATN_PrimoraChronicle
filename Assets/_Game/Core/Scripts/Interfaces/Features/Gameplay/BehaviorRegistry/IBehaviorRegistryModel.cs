using System.Collections.Generic;

public interface IBehaviorRegistryModel : IModel
{
    IReadOnlyDictionary<string, SkillBehaviorBaseSO> SkillBehaviors { get; }
    IReadOnlyDictionary<string, StatusEffectBehaviorBaseSO> StatusEffectBehaviors { get; }
    IReadOnlyDictionary<string, MainPhaseSpellBehaviorBaseSO> MainPhaseSpellBehaviors { get; }
    IReadOnlyDictionary<string, EvolutionBehaviorBaseSO> EvolutionBehaviors { get; }

    void ApplyBehaviors(
        IReadOnlyDictionary<string, SkillBehaviorBaseSO> skills,
        IReadOnlyDictionary<string, StatusEffectBehaviorBaseSO> effects,
        IReadOnlyDictionary<string, MainPhaseSpellBehaviorBaseSO> spells,
        IReadOnlyDictionary<string, EvolutionBehaviorBaseSO> evolutions);
}
