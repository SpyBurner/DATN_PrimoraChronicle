using Fusion;
using UnityEngine;
using Zenject;

public class DamagePipelineSubsystem : IDamagePipelineSubsystem
{
    [Inject] private readonly IUnitSubsystem _unitSubsystem;
    [Inject] private readonly ITileEffectSubsystem _tileEffectSubsystem;
    [Inject(Optional = true)] private readonly IDebugLogger _logger;

    public void Initialize() { }

    public void Dispose() { }

    public int Resolve(DamageContext context)
    {
        // Pass 1: Aggregate
        int aggregated = Aggregate(context);
        if (aggregated <= 0) return 0;

        // Pass 2: Intercept (tile effects first, then unit status effects)
        int intercepted = Intercept(context, aggregated);

        // Pass 3: Commit (clamp to 0 minimum)
        int final = Mathf.Max(0, intercepted);

        _logger?.Log($"[DamagePipeline] {context.SourceUnitId} → {context.TargetUnitId}: Raw={context.RawAmount}, Aggregated={aggregated}, Intercepted={intercepted}, Final={final}");

        return final;
    }

    private int Aggregate(DamageContext context)
    {
        int amount = context.RawAmount;

        // F4.11: Friendly-fire check
        if (!context.IsAOE && !string.IsNullOrEmpty(context.SourceUnitId) && !string.IsNullOrEmpty(context.TargetUnitId))
        {
            if (TryGetPublic(context.SourceUnitId, out UnitPublicData sourceData)
                && TryGetPublic(context.TargetUnitId, out UnitPublicData targetData))
            {
                if (sourceData.Owner == targetData.Owner)
                    return 0; // Block friendly fire by default
            }
        }

        return amount;
    }

    private int Intercept(DamageContext context, int currentAmount)
    {
        int result = currentAmount;

        // Tile effects evaluated FIRST
        result = InterceptByTileEffects(context, result);

        // Unit status effects evaluated SECOND
        result = InterceptByStatusEffects(context, result);

        return result;
    }

    private int InterceptByTileEffects(DamageContext context, int currentAmount)
    {
        if (_tileEffectSubsystem == null) return currentAmount;

        // Check if target is standing on a defensive tile effect
        if (_tileEffectSubsystem.TryGet(context.TargetPosition, out TileEffectInstance effect))
        {
            // Owner's units are immune to their own negative tile effects
            if (!string.IsNullOrEmpty(context.TargetUnitId)
                && TryGetPublic(context.TargetUnitId, out UnitPublicData targetData))
            {
                if (effect.Owner == targetData.Owner)
                {
                    // Friendly tile — check for defensive buffs
                    switch (effect.EffectId)
                    {
                        case "Seeded":
                            // Seeded tiles give minor damage reduction to owner's units
                            currentAmount = Mathf.Max(0, currentAmount - 5);
                            break;
                    }
                }
            }
        }

        return currentAmount;
    }

    private int InterceptByStatusEffects(DamageContext context, int currentAmount)
    {
        if (string.IsNullOrEmpty(context.TargetUnitId)) return currentAmount;
        if (!TryGetPublic(context.TargetUnitId, out UnitPublicData targetData)) return currentAmount;
        if (targetData.StatusEffects == null) return currentAmount;

        foreach (var status in targetData.StatusEffects)
        {
            switch (status.StatusId)
            {
                case "barkskin_ward":
                    // Reduce incoming damage by 15
                    currentAmount = Mathf.Max(0, currentAmount - 15);
                    _logger?.Log($"[DamagePipeline] barkskin_ward intercepted: reduced by 15.");
                    break;

                case "decay":
                    // Decay blocks healing but does not reduce damage
                    break;
            }
        }

        return currentAmount;
    }

    private bool TryGetPublic(string unitId, out UnitPublicData data)
    {
        data = default;
        if (string.IsNullOrEmpty(unitId)) return false;
        if (!uint.TryParse(unitId, out uint raw)) return false;
        var netId = new NetworkId { Raw = raw };
        return _unitSubsystem != null && _unitSubsystem.TryGetPublic(netId, out data);
    }
}
