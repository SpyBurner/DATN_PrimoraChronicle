using UnityEngine;

[CreateAssetMenu(fileName = "GenericStatusEffectBehavior", menuName = "Primora/Behaviors/GenericStatusEffectBehavior")]
public class GenericStatusEffectBehaviorSO : StatusEffectBehaviorBaseSO
{
    public void OnTurnStart(string unitId, IUnitSubsystem units, IDamagePipelineSubsystem damagePipeline)
    {
        if (damagePerTurn > 0)
        {
            var context = new DamageContext
            {
                SourceUnitId = null,
                TargetUnitId = unitId,
                RawAmount = damagePerTurn,
                SourceSkillId = effectId,
                IsAOE = false
            };
            damagePipeline.Resolve(context);
        }

        if (healPerTurn > 0 && !preventsHealing)
        {
            Debug.Log($"[StatusEffect] {effectId} heals {unitId} for {healPerTurn}");
        }
    }

    public int Intercept(DamageContext context)
    {
        if (interceptAmount > 0)
        {
            int reduction = Mathf.Min(interceptAmount, context.RawAmount);
            Debug.Log($"[StatusEffect] {effectId} intercepts {reduction} damage on {context.TargetUnitId}");
            return reduction;
        }
        return 0;
    }

    public void OnUnitMove(string unitId, HexCoord from, HexCoord to, ITileEffectSubsystem tileEffects)
    {
        if (leavesTrailOnMove && !string.IsNullOrEmpty(trailTileEffectId))
        {
            Debug.Log($"[StatusEffect] {effectId} leaves trail {trailTileEffectId} at ({from.P},{from.Q})");
        }
    }
}
