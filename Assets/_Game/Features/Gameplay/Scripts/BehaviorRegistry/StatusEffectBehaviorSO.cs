using UnityEngine;

[CreateAssetMenu(fileName = "StatusEffectBehavior", menuName = "Primora/StatusEffects/StatusEffectBehavior")]
public class StatusEffectBehaviorSO : ScriptableObject
{
    [Header("Behavior Settings")]
    public string behaviorId;

    [Header("Damage Over Time")]
    public int damagePerTurn = 0;

    [Header("Damage Intercept")]
    public int damageReduction = 0;

    [Header("Movement")]
    public bool preventsMovement = false;

    [Header("Healing")]
    public bool blocksHealing = false;

    [Header("Trail")]
    public bool leavesTrailOnMove = false;
    public string trailEffectId = "";
    public int trailDuration = 1;

    public virtual int OnStartOfTurn(UnitNetworkView unitView, IDebugLogger logger)
    {
        if (damagePerTurn > 0)
        {
            logger?.Log($"[StatusEffect] '{behaviorId}' deals {damagePerTurn} to unit {unitView.UnitId}.");
            return damagePerTurn;
        }
        return 0;
    }

    public virtual int InterceptDamage(int incomingDamage, IDebugLogger logger)
    {
        if (damageReduction > 0)
        {
            int reduced = Mathf.Max(0, incomingDamage - damageReduction);
            logger?.Log($"[StatusEffect] '{behaviorId}' reduced damage by {damageReduction}. {incomingDamage} → {reduced}.");
            return reduced;
        }
        return incomingDamage;
    }
}
