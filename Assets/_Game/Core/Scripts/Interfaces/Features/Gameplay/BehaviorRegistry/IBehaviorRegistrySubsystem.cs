public interface IBehaviorRegistrySubsystem : ISubsystem
{
    bool TryGetSkillBehavior(string behaviorId, out SkillBehaviorBaseSO behavior);
    bool TryGetStatusEffectBehavior(string behaviorId, out StatusEffectBehaviorBaseSO behavior);
    bool TryGetMainPhaseSpellBehavior(string behaviorId, out MainPhaseSpellBehaviorBaseSO behavior);
    bool TryGetEvolutionBehavior(string behaviorId, out EvolutionBehaviorBaseSO behavior);
    void LoadAll();
}
