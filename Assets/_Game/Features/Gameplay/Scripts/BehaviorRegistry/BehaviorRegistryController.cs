using System.Collections.Generic;
using UnityEngine;
using Zenject;

internal class BehaviorRegistryController : IBehaviorRegistryController
{
    [Inject] private readonly IBehaviorRegistryModel _model;
    [Inject] private readonly IDebugLogger _logger;

    private const string SkillBehaviorsPath = "Behaviors/Skills";
    private const string StatusEffectBehaviorsPath = "Behaviors/StatusEffects";
    private const string MainPhaseSpellBehaviorsPath = "Behaviors/MainPhaseSpells";
    private const string EvolutionBehaviorsPath = "Behaviors/Evolutions";

    public void Initialize() { }

    public void Dispose() { }

    public void LoadAll()
    {
        var skills = new Dictionary<string, SkillBehaviorBaseSO>();
        var effects = new Dictionary<string, StatusEffectBehaviorBaseSO>();
        var spells = new Dictionary<string, MainPhaseSpellBehaviorBaseSO>();
        var evolutions = new Dictionary<string, EvolutionBehaviorBaseSO>();

        foreach (var so in Resources.LoadAll<SkillBehaviorBaseSO>(SkillBehaviorsPath))
        {
            if (string.IsNullOrEmpty(so.behaviorId))
            {
                _logger.LogWarning($"[BehaviorRegistry] SkillBehavior asset '{so.name}' has empty behaviorId — skipped.");
                continue;
            }
            if (skills.ContainsKey(so.behaviorId))
            {
                _logger.LogWarning($"[BehaviorRegistry] Duplicate skill behaviorId '{so.behaviorId}' — skipped '{so.name}'.");
                continue;
            }
            skills[so.behaviorId] = so;
        }

        foreach (var so in Resources.LoadAll<StatusEffectBehaviorBaseSO>(StatusEffectBehaviorsPath))
        {
            if (string.IsNullOrEmpty(so.effectId))
            {
                _logger.LogWarning($"[BehaviorRegistry] StatusEffectBehavior asset '{so.name}' has empty effectId — skipped.");
                continue;
            }
            if (effects.ContainsKey(so.effectId))
            {
                _logger.LogWarning($"[BehaviorRegistry] Duplicate effect effectId '{so.effectId}' — skipped '{so.name}'.");
                continue;
            }
            effects[so.effectId] = so;
        }

        foreach (var so in Resources.LoadAll<MainPhaseSpellBehaviorBaseSO>(MainPhaseSpellBehaviorsPath))
        {
            if (string.IsNullOrEmpty(so.behaviorId))
            {
                _logger.LogWarning($"[BehaviorRegistry] MainPhaseSpellBehavior asset '{so.name}' has empty behaviorId — skipped.");
                continue;
            }
            if (spells.ContainsKey(so.behaviorId))
            {
                _logger.LogWarning($"[BehaviorRegistry] Duplicate spell behaviorId '{so.behaviorId}' — skipped '{so.name}'.");
                continue;
            }
            spells[so.behaviorId] = so;
        }

        foreach (var so in Resources.LoadAll<EvolutionBehaviorBaseSO>(EvolutionBehaviorsPath))
        {
            if (string.IsNullOrEmpty(so.behaviorId))
            {
                _logger.LogWarning($"[BehaviorRegistry] EvolutionBehavior asset '{so.name}' has empty behaviorId — skipped.");
                continue;
            }
            if (evolutions.ContainsKey(so.behaviorId))
            {
                _logger.LogWarning($"[BehaviorRegistry] Duplicate evolution behaviorId '{so.behaviorId}' — skipped '{so.name}'.");
                continue;
            }
            evolutions[so.behaviorId] = so;
        }

        _model.ApplyBehaviors(skills, effects, spells, evolutions);
        _logger.Log($"[BehaviorRegistry] Loaded {skills.Count} skills, {effects.Count} effects, {spells.Count} spells, {evolutions.Count} evolutions.");
    }

    public bool TryGetSkillBehavior(string behaviorId, out SkillBehaviorBaseSO behavior)
        => _model.SkillBehaviors.TryGetValue(behaviorId, out behavior);

    public bool TryGetStatusEffectBehavior(string behaviorId, out StatusEffectBehaviorBaseSO behavior)
        => _model.StatusEffectBehaviors.TryGetValue(behaviorId, out behavior);

    public bool TryGetMainPhaseSpellBehavior(string behaviorId, out MainPhaseSpellBehaviorBaseSO behavior)
        => _model.MainPhaseSpellBehaviors.TryGetValue(behaviorId, out behavior);

    public bool TryGetEvolutionBehavior(string behaviorId, out EvolutionBehaviorBaseSO behavior)
        => _model.EvolutionBehaviors.TryGetValue(behaviorId, out behavior);
}
