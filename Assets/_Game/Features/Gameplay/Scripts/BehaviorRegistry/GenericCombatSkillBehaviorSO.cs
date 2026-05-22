using System.Collections.Generic;
using Fusion;
using UnityEngine;

[CreateAssetMenu(fileName = "GenericCombatSkillBehavior", menuName = "Primora/Skills/GenericCombatSkillBehavior")]
public class GenericCombatSkillBehaviorSO : CombatSkillBehaviorSO
{
    [Header("Summon Prefabs")]
    [SerializeField] private NetworkPrefabRef _seedlingPrefab;
    [SerializeField] private NetworkPrefabRef _ashSoldierPrefab;

    public override void Execute(SkillExecutionContext ctx)
    {
        switch (behaviorId)
        {
            case "skb_corrupted_crest":
                ExecuteCorruptedCrest(ctx);
                break;
            case "skb_graveclaw_frenzy":
                ExecuteGraveclawFrenzy(ctx);
                break;
            case "skb_deaths_toll":
                ExecuteDeathsToll(ctx);
                break;
            case "skb_cemetary":
                SpawnTileEffect(ctx, ctx.Target, "Corrupted", 3);
                break;
            case "skb_arise":
                ExecuteArise(ctx);
                break;
            case "skb_grovehearts_ascendance":
                ExecuteGroveheartsAscendance(ctx);
                break;
            case "skb_sprout":
                ExecuteSprout(ctx);
                break;
            case "skb_bloom":
                ExecuteBloom(ctx);
                break;
            case "skb_root_overgrow":
                ExecuteRootOvergrow(ctx);
                break;
            case "skb_deep_woods_entangle":
                ExecuteDeepWoodsEntangle(ctx);
                break;
            case "skb_natures_gift":
                ExecuteNaturesGift(ctx);
                break;
            case "skb_life_sapping_thorn":
                ExecuteLifeSappingThorn(ctx);
                break;
            case "skb_wild_growth":
                ExecuteWildGrowth(ctx);
                break;
            case "skb_spore_burst":
                ExecuteSporeBurst(ctx);
                break;
            case "skb_barkskin_ward":
                ExecuteBarkskinWard(ctx);
                break;
            case "skb_summon_seedling":
                ExecuteSummonSeedling(ctx);
                break;
            case "skb_mastery_of_flame":
                ExecuteMasteryOfFlame(ctx);
                break;
            case "skb_severed_tail":
                ExecuteSeveredTail(ctx);
                break;
            case "skb_banner_of_cinders":
                SpawnTileEffect(ctx, ctx.CasterData.Position, "BannerOfCinders", 4);
                break;
            case "skb_firetrap":
                ApplyStatus(ctx, ctx.CasterId, "burning_trail", 5);
                break;
            case "skb_molten_dive":
                ExecuteMoltenDive(ctx);
                break;
            case "skb_curse_of_ash":
                ExecuteCurseOfAsh(ctx);
                break;
            case "skb_legions_last_stand":
                ExecuteLegionsLastStand(ctx);
                break;
            case "skb_march_of_embers":
                ExecuteMarchOfEmbers(ctx);
                break;
            default:
                ctx.Logger?.LogWarning($"[GenericCombatSkill] Unknown behavior: '{behaviorId}'");
                break;
        }
    }

    private void ExecuteCorruptedCrest(SkillExecutionContext ctx)
    {
        if (ctx.TileEffectSubsystem.TryGet(ctx.Target, out TileEffectInstance existing)
            && existing.EffectId == "Corrupted")
        {
            foreach (var neighbor in ctx.BoardSubsystem.GetNeighbors(ctx.Target))
                SpawnTileEffect(ctx, neighbor, "Corrupted", 3);
        }
        else
        {
            SpawnTileEffect(ctx, ctx.Target, "Corrupted", 3);
        }
    }

    private void ExecuteGraveclawFrenzy(SkillExecutionContext ctx)
    {
        string targetId = FindUnitAtPosition(ctx, ctx.Target);
        if (string.IsNullOrEmpty(targetId)) return;

        if (!ctx.UnitSubsystem.TryGetUnit(targetId, out var targetData)) return;
        if (targetData.Owner == ctx.CasterData.Owner) return;

        DealDamage(ctx, targetId, 15);
        DealDamage(ctx, targetId, 15);
    }

    private void ExecuteDeathsToll(SkillExecutionContext ctx)
    {
        var tilesInRange = ctx.BoardSubsystem.GetTilesInRange(ctx.CasterData.Position, 2);
        foreach (var tile in tilesInRange)
        {
            string unitId = FindUnitAtPosition(ctx, tile);
            if (string.IsNullOrEmpty(unitId)) continue;

            if (ctx.UnitSubsystem.TryGetUnit(unitId, out var data) && data.Owner != ctx.CasterData.Owner)
            {
                DealDamage(ctx, unitId, 12);
                DealDamage(ctx, unitId, 12);
            }
        }
    }

    private void ExecuteArise(SkillExecutionContext ctx)
    {
        string targetId = FindUnitAtPosition(ctx, ctx.Target);
        if (string.IsNullOrEmpty(targetId)) return;

        if (!ctx.UnitSubsystem.TryGetUnit(targetId, out var targetData)) return;
        if (targetData.Owner == ctx.CasterData.Owner) return;

        ApplyStatus(ctx, targetId, "decay", 3);
        SpawnTileEffect(ctx, ctx.Target, "Corrupted", 3);
    }

    private void ExecuteGroveheartsAscendance(SkillExecutionContext ctx)
    {
        if (ctx.TileEffectSubsystem.TryGet(ctx.Target, out TileEffectInstance existing)
            && existing.EffectId == "Seeded")
        {
            // Growth stack to ally on tile
            string targetId = FindUnitAtPosition(ctx, ctx.Target);
            if (!string.IsNullOrEmpty(targetId))
            {
                var view = FindUnitView(ctx, targetId);
                view?.ServerAddGrowthStack(1);
            }

            // Heal allies in 1-hex range for 20
            foreach (var neighbor in ctx.BoardSubsystem.GetNeighbors(ctx.Target))
            {
                string allyId = FindUnitAtPosition(ctx, neighbor);
                if (!string.IsNullOrEmpty(allyId) && ctx.UnitSubsystem.TryGetUnit(allyId, out var allyData)
                    && allyData.Owner == ctx.CasterData.Owner)
                {
                    HealUnit(ctx, allyId, 20);
                }
            }

            // Seed random adjacent
            var neighbors = ctx.BoardSubsystem.GetNeighbors(ctx.Target);
            if (neighbors.Count > 0)
            {
                var randomNeighbor = neighbors[Random.Range(0, neighbors.Count)];
                SpawnTileEffect(ctx, randomNeighbor, "Seeded", 4);
            }
        }
        else
        {
            SpawnTileEffect(ctx, ctx.Target, "Seeded", 4);
        }
    }

    private void ExecuteSprout(SkillExecutionContext ctx)
    {
        var casterView = FindUnitView(ctx, ctx.CasterId);
        casterView?.ServerAddGrowthStack(1);
    }

    private void ExecuteBloom(SkillExecutionContext ctx)
    {
        HealUnit(ctx, ctx.CasterId, 10);
        foreach (var neighbor in ctx.BoardSubsystem.GetNeighbors(ctx.CasterData.Position))
        {
            string allyId = FindUnitAtPosition(ctx, neighbor);
            if (!string.IsNullOrEmpty(allyId) && ctx.UnitSubsystem.TryGetUnit(allyId, out var allyData)
                && allyData.Owner == ctx.CasterData.Owner)
            {
                HealUnit(ctx, allyId, 10);
            }
        }
    }

    private void ExecuteRootOvergrow(SkillExecutionContext ctx)
    {
        string targetId = FindUnitAtPosition(ctx, ctx.Target);
        if (string.IsNullOrEmpty(targetId)) return;

        if (ctx.UnitSubsystem.TryGetUnit(targetId, out var data) && data.Owner != ctx.CasterData.Owner)
            ApplyStatus(ctx, targetId, "rooted", 3);
    }

    private void ExecuteDeepWoodsEntangle(SkillExecutionContext ctx)
    {
        foreach (var neighbor in ctx.BoardSubsystem.GetNeighbors(ctx.CasterData.Position))
            SpawnTileEffect(ctx, neighbor, "Entangled", 1);
    }

    private void ExecuteNaturesGift(SkillExecutionContext ctx)
    {
        string targetId = FindUnitAtPosition(ctx, ctx.Target);
        if (string.IsNullOrEmpty(targetId)) return;

        if (ctx.UnitSubsystem.TryGetUnit(targetId, out var data) && data.Owner == ctx.CasterData.Owner)
        {
            var view = FindUnitView(ctx, targetId);
            view?.ServerAddGrowthStack(1);
        }
    }

    private void ExecuteLifeSappingThorn(SkillExecutionContext ctx)
    {
        string targetId = FindUnitAtPosition(ctx, ctx.Target);
        if (string.IsNullOrEmpty(targetId)) return;

        if (ctx.UnitSubsystem.TryGetUnit(targetId, out var data) && data.Owner != ctx.CasterData.Owner)
        {
            DealDamage(ctx, targetId, 15);

            // If caster is on seeded tile, heal 20
            if (ctx.TileEffectSubsystem.TryGet(ctx.CasterData.Position, out var effect) && effect.EffectId == "Seeded")
                HealUnit(ctx, ctx.CasterId, 20);
        }
    }

    private void ExecuteWildGrowth(SkillExecutionContext ctx)
    {
        if (ctx.TileEffectSubsystem.TryGet(ctx.Target, out var existing) && existing.EffectId == "Seeded")
        {
            string targetId = FindUnitAtPosition(ctx, ctx.Target);
            if (!string.IsNullOrEmpty(targetId) && ctx.UnitSubsystem.TryGetUnit(targetId, out var data)
                && data.Owner == ctx.CasterData.Owner)
            {
                var view = FindUnitView(ctx, targetId);
                view?.ServerAddGrowthStack(1);
            }
        }
        else
        {
            SpawnTileEffect(ctx, ctx.Target, "Seeded", 4);
        }
    }

    private void ExecuteSporeBurst(SkillExecutionContext ctx)
    {
        var allUnits = ctx.UnitSubsystem.AllUnitIds;
        foreach (var id in allUnits)
        {
            if (!ctx.UnitSubsystem.TryGetUnit(id, out var data)) continue;
            if (data.Owner == ctx.CasterData.Owner) continue;
            if (data.CurrentHP <= 0) continue;

            if (ctx.TileEffectSubsystem.TryGet(data.Position, out var effect) && effect.EffectId == "Seeded")
                DealDamage(ctx, id, 15);
        }
    }

    private void ExecuteBarkskinWard(SkillExecutionContext ctx)
    {
        string targetId = FindUnitAtPosition(ctx, ctx.Target);
        if (string.IsNullOrEmpty(targetId)) return;

        if (ctx.UnitSubsystem.TryGetUnit(targetId, out var data) && data.Owner == ctx.CasterData.Owner)
            ApplyStatus(ctx, targetId, "barkskin_ward", 3);
    }

    private void ExecuteSummonSeedling(SkillExecutionContext ctx)
    {
        if (!_seedlingPrefab.IsValid) return;

        var neighbors = ctx.BoardSubsystem.GetNeighbors(ctx.CasterData.Position);
        foreach (var neighbor in neighbors)
        {
            if (ctx.BoardSubsystem.IsEmpty(neighbor))
            {
                var worldPos = ctx.BoardSubsystem.GetWorldPosition(neighbor);
                var spawnedObj = ctx.Runner.Spawn(_seedlingPrefab, worldPos, Quaternion.identity, ctx.CasterData.Owner);
                var unitView = spawnedObj.GetComponent<UnitNetworkView>();
                if (unitView != null)
                {
                    unitView.ServerInitializeFromCard(ctx.CasterData.Owner, "Seedling", null, neighbor);
                    unitView.IsPersistent = true;
                    unitView.CurrentHP = 40;
                    unitView.MaxHP = 40;
                    unitView.Speed = 2f;
                    unitView.MoveRange = 2;
                }

                ctx.BoardSubsystem.SetOccupant(neighbor, spawnedObj.Id.ToString());
                ctx.CombatView?.ServerAppendToQueue(spawnedObj.Id.ToString());
                break;
            }
        }
    }

    private void ExecuteMasteryOfFlame(SkillExecutionContext ctx)
    {
        var allUnits = ctx.UnitSubsystem.AllUnitIds;
        foreach (var id in allUnits)
        {
            if (!ctx.UnitSubsystem.TryGetUnit(id, out var data)) continue;
            if (data.CurrentHP <= 0) continue;

            if (HasStatus(ctx, id, "burning"))
            {
                SpawnTileEffect(ctx, data.Position, "Melting", 9999);
            }
            else
            {
                ApplyStatus(ctx, id, "burning", 3);
            }
        }
    }

    private void ExecuteSeveredTail(SkillExecutionContext ctx)
    {
        SpawnTileEffect(ctx, ctx.Target, "SeveredTail", 9999);

        var casterView = FindUnitView(ctx, ctx.CasterId);
        if (casterView != null)
        {
            int newMax = Mathf.Max(10, casterView.MaxHP - 10);
            int delta = casterView.MaxHP - newMax;
            casterView.MaxHP = newMax;
            casterView.CurrentHP = Mathf.Max(1, casterView.CurrentHP - delta);
        }
    }

    private void ExecuteMoltenDive(SkillExecutionContext ctx)
    {
        // Move caster to target (ignores pathfinding)
        var casterView = FindUnitView(ctx, ctx.CasterId);
        if (casterView == null) return;

        ctx.BoardSubsystem.SetOccupant(ctx.CasterData.Position, null);
        casterView.ServerMoveTo(ctx.Target);
        ctx.BoardSubsystem.SetOccupant(ctx.Target, ctx.CasterId);

        // Burn surrounding enemies
        foreach (var neighbor in ctx.BoardSubsystem.GetNeighbors(ctx.Target))
        {
            string unitId = FindUnitAtPosition(ctx, neighbor);
            if (!string.IsNullOrEmpty(unitId) && ctx.UnitSubsystem.TryGetUnit(unitId, out var data)
                && data.Owner != ctx.CasterData.Owner)
            {
                ApplyStatus(ctx, unitId, "burning", 1);
            }
        }
    }

    private void ExecuteCurseOfAsh(SkillExecutionContext ctx)
    {
        SpawnTileEffect(ctx, ctx.Target, "AshCloud", 7);
        foreach (var neighbor in ctx.BoardSubsystem.GetNeighbors(ctx.Target))
            SpawnTileEffect(ctx, neighbor, "AshCloud", 7);
    }

    private void ExecuteLegionsLastStand(SkillExecutionContext ctx)
    {
        int removedCount = 0;
        var allUnits = ctx.UnitSubsystem.AllUnitIds;

        var toRemove = new List<string>();
        foreach (var id in allUnits)
        {
            if (id == ctx.CasterId) continue;
            if (!ctx.UnitSubsystem.TryGetUnit(id, out var data)) continue;
            if (data.Owner != ctx.CasterData.Owner) continue;
            if (data.IsPersistent) continue;
            toRemove.Add(id);
        }

        foreach (var id in toRemove)
        {
            if (ctx.UnitSubsystem.TryGetUnit(id, out var data))
                ctx.BoardSubsystem.SetOccupant(data.Position, null);

            var view = FindUnitView(ctx, id);
            if (view != null && view.Object != null)
                ctx.Runner.Despawn(view.Object);
            removedCount++;
        }

        if (removedCount > 0)
        {
            ApplyStatus(ctx, ctx.CasterId, "legions_buff", 5);
            HealUnit(ctx, ctx.CasterId, removedCount * 15);
        }
    }

    private void ExecuteMarchOfEmbers(SkillExecutionContext ctx)
    {
        if (!_ashSoldierPrefab.IsValid) return;

        int spawned = 0;
        var allTiles = ctx.BoardSubsystem.AllTiles;

        foreach (var tile in allTiles)
        {
            if (!ctx.BoardSubsystem.IsEmpty(tile)) continue;

            var worldPos = ctx.BoardSubsystem.GetWorldPosition(tile);
            var obj = ctx.Runner.Spawn(_ashSoldierPrefab, worldPos, Quaternion.identity, ctx.CasterData.Owner);
            var unitView = obj.GetComponent<UnitNetworkView>();
            if (unitView != null)
            {
                unitView.ServerInitializeFromCard(ctx.CasterData.Owner, "AshSoldier", null, tile);
                unitView.CurrentHP = 30;
                unitView.MaxHP = 30;
                unitView.Speed = 3f;
                unitView.MoveRange = 3;
                unitView.IsPersistent = false;
            }

            ctx.BoardSubsystem.SetOccupant(tile, obj.Id.ToString());
            ctx.CombatView?.ServerAppendToQueue(obj.Id.ToString());
            spawned++;
            if (spawned >= 4) break;
        }
    }
}
