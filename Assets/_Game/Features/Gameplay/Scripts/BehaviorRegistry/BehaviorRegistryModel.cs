using System.Collections.Generic;

internal class BehaviorRegistryModel : IBehaviorRegistryModel
{
    private Dictionary<string, SkillBehaviorBaseSO> _skills = new();
    private Dictionary<string, StatusEffectBehaviorBaseSO> _effects = new();
    private Dictionary<string, MainPhaseSpellBehaviorBaseSO> _spells = new();
    private Dictionary<string, EvolutionBehaviorBaseSO> _evolutions = new();

    public IReadOnlyDictionary<string, SkillBehaviorBaseSO> SkillBehaviors => _skills;
    public IReadOnlyDictionary<string, StatusEffectBehaviorBaseSO> StatusEffectBehaviors => _effects;
    public IReadOnlyDictionary<string, MainPhaseSpellBehaviorBaseSO> MainPhaseSpellBehaviors => _spells;
    public IReadOnlyDictionary<string, EvolutionBehaviorBaseSO> EvolutionBehaviors => _evolutions;

    public void Initialize() { }

    public void Dispose()
    {
        _skills.Clear();
        _effects.Clear();
        _spells.Clear();
        _evolutions.Clear();
    }

    public void ApplyBehaviors(
        IReadOnlyDictionary<string, SkillBehaviorBaseSO> skills,
        IReadOnlyDictionary<string, StatusEffectBehaviorBaseSO> effects,
        IReadOnlyDictionary<string, MainPhaseSpellBehaviorBaseSO> spells,
        IReadOnlyDictionary<string, EvolutionBehaviorBaseSO> evolutions)
    {
        _skills = new Dictionary<string, SkillBehaviorBaseSO>(skills);
        _effects = new Dictionary<string, StatusEffectBehaviorBaseSO>(effects);
        _spells = new Dictionary<string, MainPhaseSpellBehaviorBaseSO>(spells);
        _evolutions = new Dictionary<string, EvolutionBehaviorBaseSO>(evolutions);
    }
}
