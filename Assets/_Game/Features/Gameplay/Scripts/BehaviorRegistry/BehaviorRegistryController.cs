using System.Collections.Generic;
using UnityEngine;
using Zenject;

internal class BehaviorRegistryController : IBehaviorRegistryController
{
    private const string SkillBehaviorsPath = "Behaviors/Skills";
    private const string StatusEffectBehaviorsPath = "Behaviors/StatusEffects";
    private const string MainPhaseSpellBehaviorsPath = "Behaviors/Spells";

    [Inject] private readonly IBehaviorRegistryModel _model;
    [Inject] private readonly IDebugLogger _logger;

    public void Initialize()
    {
        var skills = LoadAndIndex<CombatSkillBehaviorSO>(SkillBehaviorsPath, so => so.behaviorId);
        var effects = LoadAndIndex<StatusEffectBehaviorSO>(StatusEffectBehaviorsPath, so => so.behaviorId);
        var spells = LoadAndIndex<MainPhaseSpellBehaviorSO>(MainPhaseSpellBehaviorsPath, so => so.behaviorId);

        _model.ApplyBehaviors(skills, effects, spells);

        _logger?.Log($"[BehaviorRegistry] Loaded {skills.Count} skills, {effects.Count} effects, {spells.Count} spells.");
    }

    public void Dispose() { }

    public bool TryGetSkillBehavior(string behaviorId, out ScriptableObject behavior)
        => _model.SkillBehaviors.TryGetValue(behaviorId, out behavior);

    public bool TryGetStatusEffectBehavior(string behaviorId, out ScriptableObject behavior)
        => _model.StatusEffectBehaviors.TryGetValue(behaviorId, out behavior);

    public bool TryGetMainPhaseSpellBehavior(string behaviorId, out ScriptableObject behavior)
        => _model.MainPhaseSpellBehaviors.TryGetValue(behaviorId, out behavior);

    private Dictionary<string, ScriptableObject> LoadAndIndex<T>(string resourcePath, System.Func<T, string> idSelector)
        where T : ScriptableObject
    {
        var result = new Dictionary<string, ScriptableObject>();
        var assets = Resources.LoadAll<T>(resourcePath);

        if (assets == null || assets.Length == 0)
        {
            _logger?.LogWarning($"[BehaviorRegistry] No assets found at Resources/{resourcePath}");
            return result;
        }

        foreach (var asset in assets)
        {
            string id = idSelector(asset);
            if (string.IsNullOrEmpty(id))
            {
                _logger?.LogWarning($"[BehaviorRegistry] Asset '{asset.name}' at Resources/{resourcePath} has empty behaviorId. Skipping.");
                continue;
            }

            if (result.ContainsKey(id))
            {
                _logger?.LogWarning($"[BehaviorRegistry] Duplicate behaviorId '{id}' at Resources/{resourcePath}. Asset '{asset.name}' skipped.");
                continue;
            }

            result[id] = asset;
        }

        return result;
    }
}
