using Fusion;
using UnityEngine;

[CreateAssetMenu(fileName = "CombatSkillBehavior", menuName = "Primora/Skills/CombatSkillBehavior")]
public class CombatSkillBehaviorSO : SkillBehaviorBaseSO
{
    public virtual void Execute(CombatSkillExecutionContext ctx)
    {
        Debug.LogWarning($"[CombatSkillBehaviorSO] Base Execute called for '{behaviorId}'. Override in subclass.");
    }

    protected void DealDamage(CombatSkillExecutionContext ctx, string targetUnitId, int rawAmount)
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

    protected void DealDamageIgnoreFriendly(CombatSkillExecutionContext ctx, string targetUnitId, int rawAmount)
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

    protected void HealUnit(CombatSkillExecutionContext ctx, string unitId, int amount)
    {
        if (HasStatus(ctx, unitId, "decay")) return; // Decay blocks healing

        var view = FindUnitView(ctx, unitId);
        view?.ServerHeal(amount);
    }

    protected void ApplyStatus(CombatSkillExecutionContext ctx, string targetUnitId, string statusId, int duration)
    {
        var view = FindUnitView(ctx, targetUnitId);
        view?.ServerAddStatus(statusId, duration, ctx.CasterData.Owner);
    }

    protected void SpawnTileEffect(CombatSkillExecutionContext ctx, HexCoord coord, string effectId, int duration)
    {
        ctx.TileEffectSubsystem?.OnEffectReceived(new TileEffectInstance
        {
            Position = coord,
            EffectId = effectId,
            DurationRemaining = duration,
            Owner = ctx.CasterData.Owner
        });
    }

    protected string FindUnitAtPosition(CombatSkillExecutionContext ctx, HexCoord position)
    {
        var allUnits = ctx.UnitSubsystem?.AllUnits;
        if (allUnits == null) return null;

        foreach (var netId in allUnits)
        {
            string id = netId.ToString();
            if (TryGetPublic(ctx, id, out UnitPublicData data) && data.Position == position && data.CurrentHP > 0)
                return id;
        }
        return null;
    }

    protected bool HasStatus(CombatSkillExecutionContext ctx, string unitId, string statusId)
    {
        if (!TryGetPublic(ctx, unitId, out UnitPublicData data)) return false;
        if (data.StatusEffects == null) return false;
        foreach (var s in data.StatusEffects)
            if (s.StatusId == statusId) return true;
        return false;
    }

    protected UnitNetworkView FindUnitView(CombatSkillExecutionContext ctx, string unitId)
    {
        if (string.IsNullOrEmpty(unitId) || !ctx.Runner.IsRunning) return null;
        if (uint.TryParse(unitId, out uint raw))
        {
            var netId = new NetworkId { Raw = raw };
            if (ctx.Runner.TryFindObject(netId, out var netObj))
                return netObj.GetComponent<UnitNetworkView>();
        }
        return null;
    }

    private HexCoord GetUnitPosition(CombatSkillExecutionContext ctx, string unitId)
    {
        if (TryGetPublic(ctx, unitId, out UnitPublicData data))
            return data.Position;
        return HexCoord.Invalid;
    }

    protected bool TryGetPublic(CombatSkillExecutionContext ctx, string unitId, out UnitPublicData data)
    {
        data = default;
        if (string.IsNullOrEmpty(unitId)) return false;
        if (!uint.TryParse(unitId, out uint raw)) return false;
        var netId = new NetworkId { Raw = raw };
        return ctx.UnitSubsystem != null && ctx.UnitSubsystem.TryGetPublic(netId, out data);
    }
}
