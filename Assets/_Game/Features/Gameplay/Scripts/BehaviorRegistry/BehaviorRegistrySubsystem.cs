using System;
using UnityEngine;
using Zenject;

public class BehaviorRegistrySubsystem : IBehaviorRegistrySubsystem
{
    [Inject] private readonly IBehaviorRegistryController _controller;
    [Inject] private readonly IBehaviorRegistryModel _model;
    [Inject] private readonly IDebugLogger _logger;

    public void Initialize()
    {
        _controller.Initialize();
        LoadAll();
    }

    public void Dispose()
    {
        _controller.Dispose();
        _model.Dispose();
    }

    public void LoadAll()
    {
        try
        {
            _controller.LoadAll();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    public bool TryGetSkillBehavior(string behaviorId, out SkillBehaviorBaseSO behavior)
        => _controller.TryGetSkillBehavior(behaviorId, out behavior);

    public bool TryGetStatusEffectBehavior(string behaviorId, out StatusEffectBehaviorBaseSO behavior)
        => _controller.TryGetStatusEffectBehavior(behaviorId, out behavior);

    public bool TryGetMainPhaseSpellBehavior(string behaviorId, out MainPhaseSpellBehaviorBaseSO behavior)
        => _controller.TryGetMainPhaseSpellBehavior(behaviorId, out behavior);

    public bool TryGetEvolutionBehavior(string behaviorId, out EvolutionBehaviorBaseSO behavior)
        => _controller.TryGetEvolutionBehavior(behaviorId, out behavior);
}
