using System.Collections.Generic;
using UnityEngine;

internal class BehaviorRegistryModel : IBehaviorRegistryModel
{
    private Dictionary<string, ScriptableObject> _skills = new();
    private Dictionary<string, ScriptableObject> _effects = new();
    private Dictionary<string, ScriptableObject> _spells = new();

    public IReadOnlyDictionary<string, ScriptableObject> SkillBehaviors => _skills;
    public IReadOnlyDictionary<string, ScriptableObject> StatusEffectBehaviors => _effects;
    public IReadOnlyDictionary<string, ScriptableObject> MainPhaseSpellBehaviors => _spells;

    public void Initialize() { }

    public void Dispose()
    {
        _skills.Clear();
        _effects.Clear();
        _spells.Clear();
    }

    public void ApplyBehaviors(
        IReadOnlyDictionary<string, ScriptableObject> skills,
        IReadOnlyDictionary<string, ScriptableObject> effects,
        IReadOnlyDictionary<string, ScriptableObject> spells)
    {
        _skills = new Dictionary<string, ScriptableObject>(skills);
        _effects = new Dictionary<string, ScriptableObject>(effects);
        _spells = new Dictionary<string, ScriptableObject>(spells);
    }
}
