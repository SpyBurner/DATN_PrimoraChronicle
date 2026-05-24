using UnityEngine;

[CreateAssetMenu(fileName = "GenericSkillBehavior", menuName = "Primora/Behaviors/GenericSkillBehavior")]
public class GenericSkillBehaviorSO : SkillBehaviorBaseSO
{
    [Header("Summon Parameters")]
    public string summonUnitCardId;

    [Header("Tile Effect Parameters")]
    public string tileEffectId;
    public int tileEffectDuration = 3;

    [Header("Damage/Heal")]
    public int directDamage;
    public int directHeal;
    public int maxHPModifier;

    [Header("Growth")]
    public int growthStacksGranted;

    public void Execute(SkillExecutionContext context, IUnitSubsystem units, IBoardSubsystem board,
        ITileEffectSubsystem tileEffects, IDamagePipelineSubsystem damagePipeline)
    {
        Debug.Log($"[SkillExecution] {behaviorId} from {context.CasterUnitId} → ({context.TargetPosition.P},{context.TargetPosition.Q})");
    }
}
