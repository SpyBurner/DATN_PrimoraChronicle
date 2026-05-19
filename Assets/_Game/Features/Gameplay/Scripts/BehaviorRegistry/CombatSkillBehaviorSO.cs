using UnityEngine;

[CreateAssetMenu(fileName = "CombatSkillBehavior", menuName = "Primora/Skills/CombatSkillBehavior")]
public class CombatSkillBehaviorSO : ScriptableObject
{
    [Header("Behavior Settings")]
    public string behaviorId;
    public int range = 1;
    public int aoe = 0;
    public int targetCondition = 1;
    public bool ignorePathfinding = false;
    public bool ignoreFriendlyFire = false;

    public virtual void Execute(SkillExecutionContext ctx)
    {
        Debug.LogWarning($"[CombatSkillBehaviorSO] Base Execute called for '{behaviorId}'. Override in subclass.");
    }

    protected void DealDamage(SkillExecutionContext ctx, string targetUnitId, int rawAmount)
    {
        var damageCtx = new DamageContext
        {
            SourceUnitId = ctx.CasterId,
            TargetUnitId = targetUnitId,
            TargetPosition = GetUnitPosition(ctx, targetUnitId),
            RawAmount = rawAmount,
            SourceSkillId = ctx.SkillData.string_id,
            IsAOE = aoe > 0
        };

        int final = ctx.DamagePipeline.Resolve(damageCtx);
        if (final > 0)
        {
            var targetView = FindUnitView(ctx, targetUnitId);
            targetView?.ServerApplyDamage(final);
        }
    }

    protected void DealDamageIgnoreFriendly(SkillExecutionContext ctx, string targetUnitId, int rawAmount)
    {
        var damageCtx = new DamageContext
        {
            SourceUnitId = ctx.CasterId,
            TargetUnitId = targetUnitId,
            TargetPosition = GetUnitPosition(ctx, targetUnitId),
            RawAmount = rawAmount,
            SourceSkillId = ctx.SkillData.string_id,
            IsAOE = true
        };

        int final = ctx.DamagePipeline.Resolve(damageCtx);
        if (final > 0)
        {
            var targetView = FindUnitView(ctx, targetUnitId);
            targetView?.ServerApplyDamage(final);
        }
    }

    protected void HealUnit(SkillExecutionContext ctx, string unitId, int amount)
    {
        if (HasStatus(ctx, unitId, "decay")) return; // Decay blocks healing

        var view = FindUnitView(ctx, unitId);
        view?.ServerHeal(amount);
    }

    protected void ApplyStatus(SkillExecutionContext ctx, string targetUnitId, string statusId, int duration)
    {
        var view = FindUnitView(ctx, targetUnitId);
        view?.ServerAddStatus(statusId, duration, ctx.CasterData.Owner);
    }

    protected void SpawnTileEffect(SkillExecutionContext ctx, HexCoord coord, string effectId, int duration)
    {
        ctx.TileEffectSubsystem?.OnEffectReceived(new TileEffectInstance
        {
            Position = coord,
            EffectId = effectId,
            DurationRemaining = duration,
            Owner = ctx.CasterData.Owner
        });
    }

    protected string FindUnitAtPosition(SkillExecutionContext ctx, HexCoord position)
    {
        var allUnits = ctx.UnitSubsystem?.AllUnitIds;
        if (allUnits == null) return null;

        foreach (var id in allUnits)
        {
            if (ctx.UnitSubsystem.TryGetUnit(id, out UnitStateData data) && data.Position == position && data.CurrentHP > 0)
                return id;
        }
        return null;
    }

    protected bool HasStatus(SkillExecutionContext ctx, string unitId, string statusId)
    {
        if (!ctx.UnitSubsystem.TryGetUnit(unitId, out UnitStateData data)) return false;
        if (data.StatusEffects == null) return false;
        foreach (var s in data.StatusEffects)
            if (s.StatusId == statusId) return true;
        return false;
    }

    protected UnitNetworkView FindUnitView(SkillExecutionContext ctx, string unitId)
    {
        if (string.IsNullOrEmpty(unitId) || !ctx.Runner.IsRunning) return null;
        if (Fusion.NetworkId.TryParse(unitId, out var netId))
        {
            if (ctx.Runner.TryFindObject(netId, out var netObj))
                return netObj.GetComponent<UnitNetworkView>();
        }
        return null;
    }

    private HexCoord GetUnitPosition(SkillExecutionContext ctx, string unitId)
    {
        if (ctx.UnitSubsystem.TryGetUnit(unitId, out UnitStateData data))
            return data.Position;
        return HexCoord.Invalid;
    }
}
