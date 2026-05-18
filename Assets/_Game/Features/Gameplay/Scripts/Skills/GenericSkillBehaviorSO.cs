using System.Collections.Generic;
using UnityEngine;
using Fusion;

[CreateAssetMenu(fileName = "GenericSkillBehavior", menuName = "Primora/Skills/GenericSkillBehavior")]
public class GenericSkillBehaviorSO : SkillBehaviorSO
{
    [Header("Skill Parameters")]
    public int range = 1;
    public int aoe = 0;
    public int targetCondition = 1; // 1: Enemy, 2: Ally, 4: EmptyTile (Bitmask)

    [Header("Prefab References for Summons")]
    public GameObject seedlingPrefab;
    public GameObject ashSoldierPrefab;
    public GameObject tileEffectPrefab; // NetworkTileEffect

    public override void Execute(NetworkGameplayManager gameplayManager, NetworkUnit caster, HexTile targetTile)
    {
        if (gameplayManager == null || caster == null || targetTile == null) return;
        if (!gameplayManager.Runner.IsServer) return; // Execute on Server only

        Debug.Log($"[SkillExecution] Caster {caster.UnitID} executing {behaviorId} on tile ({targetTile.p}, {targetTile.q})");

        switch (behaviorId)
        {
            case "skb_corrupted_crest":
                // Choose 1 tile within 3 hex range, apply status_effect_corrupted.
                // If it's already corrupted, apply to a 1 hex range area.
                ApplyCorruptedCrest(gameplayManager, caster, targetTile);
                break;

            case "skb_graveclaw_frenzy":
                // Choose enemy in 1 hex range, apply 2 normal attacks on that enemy.
                ApplyGraveclawFrenzy(gameplayManager, caster, targetTile);
                break;

            case "skb_deaths_toll":
                // Apply 2 normal attacks to all enemies within 2 hex range.
                ApplyDeathsToll(gameplayManager, caster, targetTile);
                break;

            case "skb_cemetary":
                // Choose 1 tile within 2 hex range, apply status_effect_corrupted.
                SpawnTileEffect(gameplayManager, targetTile.p, targetTile.q, "Corrupted", 3, caster.Owner);
                break;

            case "skb_arise":
                // Choose enemy unit, apply status_effect_decay to their tile.
                ApplyArise(gameplayManager, caster, targetTile);
                break;

            case "skb_grovehearts_ascendance":
                // Choose 1 tile in 3 hex range. If not seeded, apply status_effect_seeded.
                // If already seeded: Give 1 Growth Stack / Heal 20 / Seed adjacent.
                ApplyGroveheartsAscendance(gameplayManager, caster, targetTile);
                break;

            case "skb_sprout":
                // Caster gains 1 growth stack.
                caster.AddGrowthStack(1);
                break;

            case "skb_bloom":
                // Heal allies in 1 hex range for 10 HP.
                ApplyBloom(gameplayManager, caster, targetTile);
                break;

            case "skb_root_overgrow":
                // Apply status_effect_rooted to 1 adjacent enemy for 3 turns.
                ApplyRootOvergrow(gameplayManager, caster, targetTile);
                break;

            case "skb_deep_woods_entangle":
                // Start of turn: Inflict tiles in 1 hex range with status_effect_entangled for 1 turn.
                ApplyDeepWoodsEntangle(gameplayManager, caster, targetTile);
                break;

            case "skb_natures_gift":
                // Choose 1 ally within 2 hex range, give 1 Growth Stack.
                ApplyNaturesGift(gameplayManager, caster, targetTile);
                break;

            case "skb_life_sapping_thorn":
                // Apply 1 normal attack. If standing on seeded tile, heal 20 HP.
                ApplyLifeSappingThorn(gameplayManager, caster, targetTile);
                break;

            case "skb_wild_growth":
                // Apply seeded on 1 tile. If already seeded, give Growth Stack.
                ApplyWildGrowth(gameplayManager, caster, targetTile);
                break;

            case "skb_spore_burst":
                // Deal 15 damage to all enemy units standing on seeded tiles.
                ApplySporeBurst(gameplayManager, caster);
                break;

            case "skb_barkskin_ward":
                // Grant status_effect_barkskin_ward to target ally.
                ApplyBarkskinWard(gameplayManager, caster, targetTile);
                break;

            case "skb_summon_seedling":
                // Summon a Seedling NPC on a random empty tile adjacent to caster.
                SummonSeedling(gameplayManager, caster);
                break;

            case "skb_mastery_of_flame":
                // Inflict units in range with burning. If already burning, inflict melting.
                ApplyMasteryOfFlame(gameplayManager, caster, targetTile);
                break;

            case "skb_severed_tail":
                // Throw tail at tile in 5 hex range, dealing 30 damage. Caster loses 10 Max HP.
                ApplySeveredTail(gameplayManager, caster, targetTile);
                break;

            case "skb_banner_of_cinders":
                // Apply status_effect_banner_of_cinders on caster's current tile.
                SpawnTileEffect(gameplayManager, caster.P, caster.Q, "BannerOfCinders", 4, caster.Owner);
                break;

            case "skb_firetrap":
                // Apply status_effect_burning_trail on self.
                caster.ApplyStatusEffect("burning_trail", 5);
                break;

            case "skb_molten_dive":
                // Jump to empty tile in 3 hex range, deal 15 damage to surrounding.
                ApplyMoltenDive(gameplayManager, caster, targetTile);
                break;

            case "skb_curse_of_ash":
                // Rain ash cloud on selected tile and 1 hex area around it.
                ApplyCurseOfAsh(gameplayManager, caster, targetTile);
                break;

            case "skb_legions_last_stand":
                // Remove all non-champion allied units to gain buff stacks.
                ApplyLegionsLastStand(gameplayManager, caster);
                break;

            case "skb_march_of_embers":
                // Summon 4 Ash Soldiers on random empty tiles.
                SummonAshSoldiers(gameplayManager, caster);
                break;

            default:
                Debug.LogWarning($"[SkillExecution] Unknown skill behavior ID: {behaviorId}");
                break;
        }
    }

    private void SpawnTileEffect(NetworkGameplayManager gameplayManager, int p, int q, string effectType, int duration, PlayerRef owner)
    {
        // First, check if effect already exists to avoid duplication
        var existing = gameplayManager.FindTileEffectAt(p, q);
        if (existing != null)
        {
            if (existing.EffectType.ToString() == effectType)
            {
                existing.RemainingDuration = Mathf.Max(existing.RemainingDuration, duration);
                return;
            }
            else
            {
                // Overwrite old effect
                gameplayManager.Runner.Despawn(existing.Object);
            }
        }

        if (tileEffectPrefab != null)
        {
            var effectObj = gameplayManager.Runner.Spawn(tileEffectPrefab, Vector3.zero, Quaternion.identity, owner);
            var effect = effectObj.GetComponent<NetworkTileEffect>();
            if (effect != null)
            {
                effect.ApplyEffect(p, q, effectType, duration, owner);
            }
        }
    }

    private void ApplyCorruptedCrest(NetworkGameplayManager gameplayManager, NetworkUnit caster, HexTile targetTile)
    {
        var existing = gameplayManager.FindTileEffectAt(targetTile.p, targetTile.q);
        if (existing != null && existing.EffectType.ToString() == "Corrupted")
        {
            // Apply to 1 hex range area (aoe)
            var board = FindObjectOfType<BoardManager>();
            if (board != null)
            {
                foreach (var tile in GetAdjacentTiles(board, targetTile.p, targetTile.q))
                {
                    SpawnTileEffect(gameplayManager, tile.p, tile.q, "Corrupted", 3, caster.Owner);
                }
            }
        }
        else
        {
            SpawnTileEffect(gameplayManager, targetTile.p, targetTile.q, "Corrupted", 3, caster.Owner);
        }
    }

    private void ApplyGraveclawFrenzy(NetworkGameplayManager gameplayManager, NetworkUnit caster, HexTile targetTile)
    {
        var targetUnit = gameplayManager.FindUnitAtTile(targetTile.p, targetTile.q);
        if (targetUnit != null && targetUnit.Owner != caster.Owner)
        {
            targetUnit.TakeDamage(15, caster.Owner);
            targetUnit.TakeDamage(15, caster.Owner);
        }
    }

    private void ApplyDeathsToll(NetworkGameplayManager gameplayManager, NetworkUnit caster, HexTile targetTile)
    {
        var board = FindObjectOfType<BoardManager>();
        if (board == null) return;

        foreach (var tile in board.GetComponentsInChildren<HexTile>())
        {
            if (NetworkUnit.GetDistance(caster.P, caster.Q, tile.p, tile.q) <= 2)
            {
                var targetUnit = gameplayManager.FindUnitAtTile(tile.p, tile.q);
                if (targetUnit != null && targetUnit.Owner != caster.Owner)
                {
                    targetUnit.TakeDamage(12, caster.Owner);
                    targetUnit.TakeDamage(12, caster.Owner);
                }
            }
        }
    }

    private void ApplyArise(NetworkGameplayManager gameplayManager, NetworkUnit caster, HexTile targetTile)
    {
        var targetUnit = gameplayManager.FindUnitAtTile(targetTile.p, targetTile.q);
        if (targetUnit != null && targetUnit.Owner != caster.Owner)
        {
            targetUnit.ApplyStatusEffect("decay", 3);
            SpawnTileEffect(gameplayManager, targetTile.p, targetTile.q, "Corrupted", 3, caster.Owner);
        }
    }

    private void ApplyGroveheartsAscendance(NetworkGameplayManager gameplayManager, NetworkUnit caster, HexTile targetTile)
    {
        var existing = gameplayManager.FindTileEffectAt(targetTile.p, targetTile.q);
        if (existing == null || existing.EffectType.ToString() != "Seeded")
        {
            SpawnTileEffect(gameplayManager, targetTile.p, targetTile.q, "Seeded", 4, caster.Owner);
        }
        else
        {
            // Level 1: Growth stack
            var targetUnit = gameplayManager.FindUnitAtTile(targetTile.p, targetTile.q);
            if (targetUnit != null && targetUnit.Owner == caster.Owner)
            {
                targetUnit.AddGrowthStack(1);
            }

            // Level 2: Heal 20 HP to all allies in range 1
            var board = FindObjectOfType<BoardManager>();
            if (board != null)
            {
                foreach (var tile in GetAdjacentTiles(board, targetTile.p, targetTile.q))
                {
                    var unit = gameplayManager.FindUnitAtTile(tile.p, tile.q);
                    if (unit != null && unit.Owner == caster.Owner)
                    {
                        unit.Heal(20);
                    }
                }
            }

            // Level 3: Seed adjacent tile
            if (board != null)
            {
                var adj = GetAdjacentTiles(board, targetTile.p, targetTile.q);
                if (adj.Count > 0)
                {
                    var randomTile = adj[Random.Range(0, adj.Count)];
                    SpawnTileEffect(gameplayManager, randomTile.p, randomTile.q, "Seeded", 4, caster.Owner);
                }
            }
        }
    }

    private void ApplyBloom(NetworkGameplayManager gameplayManager, NetworkUnit caster, HexTile targetTile)
    {
        var board = FindObjectOfType<BoardManager>();
        if (board == null) return;

        caster.Heal(10);
        foreach (var tile in GetAdjacentTiles(board, caster.P, caster.Q))
        {
            var ally = gameplayManager.FindUnitAtTile(tile.p, tile.q);
            if (ally != null && ally.Owner == caster.Owner)
            {
                ally.Heal(10);
            }
        }
    }

    private void ApplyRootOvergrow(NetworkGameplayManager gameplayManager, NetworkUnit caster, HexTile targetTile)
    {
        var targetUnit = gameplayManager.FindUnitAtTile(targetTile.p, targetTile.q);
        if (targetUnit != null && targetUnit.Owner != caster.Owner)
        {
            targetUnit.ApplyStatusEffect("rooted", 3);
        }
    }

    private void ApplyDeepWoodsEntangle(NetworkGameplayManager gameplayManager, NetworkUnit caster, HexTile targetTile)
    {
        var board = FindObjectOfType<BoardManager>();
        if (board == null) return;

        foreach (var tile in GetAdjacentTiles(board, caster.P, caster.Q))
        {
            SpawnTileEffect(gameplayManager, tile.p, tile.q, "Entangled", 1, caster.Owner);
        }
    }

    private void ApplyNaturesGift(NetworkGameplayManager gameplayManager, NetworkUnit caster, HexTile targetTile)
    {
        var targetUnit = gameplayManager.FindUnitAtTile(targetTile.p, targetTile.q);
        if (targetUnit != null && targetUnit.Owner == caster.Owner)
        {
            targetUnit.AddGrowthStack(1);
        }
    }

    private void ApplyLifeSappingThorn(NetworkGameplayManager gameplayManager, NetworkUnit caster, HexTile targetTile)
    {
        var targetUnit = gameplayManager.FindUnitAtTile(targetTile.p, targetTile.q);
        if (targetUnit != null && targetUnit.Owner != caster.Owner)
        {
            targetUnit.TakeDamage(15, caster.Owner);

            var tileEffect = gameplayManager.FindTileEffectAt(caster.P, caster.Q);
            if (tileEffect != null && tileEffect.EffectType.ToString() == "Seeded")
            {
                caster.Heal(20);
            }
        }
    }

    private void ApplyWildGrowth(NetworkGameplayManager gameplayManager, NetworkUnit caster, HexTile targetTile)
    {
        var existing = gameplayManager.FindTileEffectAt(targetTile.p, targetTile.q);
        if (existing != null && existing.EffectType.ToString() == "Seeded")
        {
            var targetUnit = gameplayManager.FindUnitAtTile(targetTile.p, targetTile.q);
            if (targetUnit != null && targetUnit.Owner == caster.Owner)
            {
                targetUnit.AddGrowthStack(1);
            }
        }
        else
        {
            SpawnTileEffect(gameplayManager, targetTile.p, targetTile.q, "Seeded", 4, caster.Owner);
        }
    }

    private void ApplySporeBurst(NetworkGameplayManager gameplayManager, NetworkUnit caster)
    {
        foreach (var u in FindObjectsOfType<NetworkUnit>())
        {
            if (u.Owner != caster.Owner)
            {
                var effect = gameplayManager.FindTileEffectAt(u.P, u.Q);
                if (effect != null && effect.EffectType.ToString() == "Seeded")
                {
                    u.TakeDamage(15, caster.Owner);
                }
            }
        }
    }

    private void ApplyBarkskinWard(NetworkGameplayManager gameplayManager, NetworkUnit caster, HexTile targetTile)
    {
        var targetUnit = gameplayManager.FindUnitAtTile(targetTile.p, targetTile.q);
        if (targetUnit != null && targetUnit.Owner == caster.Owner)
        {
            targetUnit.ApplyStatusEffect("barkskin_ward", 3);
        }
    }

    private void SummonSeedling(NetworkGameplayManager gameplayManager, NetworkUnit caster)
    {
        var board = FindObjectOfType<BoardManager>();
        if (board == null || seedlingPrefab == null) return;

        var adjacent = GetAdjacentTiles(board, caster.P, caster.Q);
        foreach (var tile in adjacent)
        {
            if (gameplayManager.FindUnitAtTile(tile.p, tile.q) == null)
            {
                Vector3 worldPos = board.ResolveCoordinateToPosition(tile.p, tile.q);
                var seedlingObj = gameplayManager.Runner.Spawn(seedlingPrefab, worldPos, Quaternion.identity, caster.Owner);
                var unit = seedlingObj.GetComponent<NetworkUnit>();
                if (unit != null)
                {
                    unit.InitializeUnit(caster.Owner, "Seedling", 40, 2f, 1, 2, caster.Faction.ToString(), false);
                    unit.P = tile.p;
                    unit.Q = tile.q;
                }
                break;
            }
        }
    }

    private void ApplyMasteryOfFlame(NetworkGameplayManager gameplayManager, NetworkUnit caster, HexTile targetTile)
    {
        var targetUnit = gameplayManager.FindUnitAtTile(targetTile.p, targetTile.q);
        if (targetUnit != null)
        {
            if (targetUnit.HasStatusEffect("burning"))
            {
                targetUnit.ApplyStatusEffect("melting", 3);
            }
            else
            {
                targetUnit.ApplyStatusEffect("burning", 3);
            }
        }
    }

    private void ApplySeveredTail(NetworkGameplayManager gameplayManager, NetworkUnit caster, HexTile targetTile)
    {
        var targetUnit = gameplayManager.FindUnitAtTile(targetTile.p, targetTile.q);
        if (targetUnit != null && targetUnit.Owner != caster.Owner)
        {
            targetUnit.TakeDamage(30, caster.Owner);
            caster.MaxHP = Mathf.Max(10, caster.MaxHP - 6);
            caster.HP = Mathf.Min(caster.HP, caster.MaxHP);
        }
    }

    private void ApplyMoltenDive(NetworkGameplayManager gameplayManager, NetworkUnit caster, HexTile targetTile)
    {
        var board = FindObjectOfType<BoardManager>();
        if (board == null) return;

        // Move caster (ignore pathfinding)
        caster.P = targetTile.p;
        caster.Q = targetTile.q;
        Vector3 worldPos = board.ResolveCoordinateToPosition(caster.P, caster.Q);
        caster.transform.position = new Vector3(worldPos.x, caster.transform.position.y, worldPos.z);

        // Apply burning to surrounding 1 hex ring (excluding caster tile)
        foreach (var tile in GetAdjacentTiles(board, targetTile.p, targetTile.q))
        {
            var u = gameplayManager.FindUnitAtTile(tile.p, tile.q);
            if (u != null && u.Owner != caster.Owner)
            {
                u.ApplyStatusEffect("burning", 1);
            }
        }
    }

    private void ApplyCurseOfAsh(NetworkGameplayManager gameplayManager, NetworkUnit caster, HexTile targetTile)
    {
        var board = FindObjectOfType<BoardManager>();
        if (board == null) return;

        SpawnTileEffect(gameplayManager, targetTile.p, targetTile.q, "AshCloud", 7, caster.Owner);
        foreach (var tile in GetAdjacentTiles(board, targetTile.p, targetTile.q))
        {
            SpawnTileEffect(gameplayManager, tile.p, tile.q, "AshCloud", 7, caster.Owner);
        }
    }

    private void ApplyLegionsLastStand(NetworkGameplayManager gameplayManager, NetworkUnit caster)
    {
        int removedCount = 0;
        foreach (var u in FindObjectsOfType<NetworkUnit>())
        {
            if (u.Owner == caster.Owner && u != caster && !u.IsPersistent)
            {
                gameplayManager.Runner.Despawn(u.Object);
                removedCount++;
            }
        }

        if (removedCount > 0)
        {
            caster.ApplyStatusEffect("legions_buff", 5); // Give strong offensive buff
            caster.Heal(removedCount * 15);
        }
    }

    private void SummonAshSoldiers(NetworkGameplayManager gameplayManager, NetworkUnit caster)
    {
        var board = FindObjectOfType<BoardManager>();
        if (board == null || ashSoldierPrefab == null) return;

        int spawned = 0;
        foreach (var tile in board.GetComponentsInChildren<HexTile>())
        {
            if (gameplayManager.FindUnitAtTile(tile.p, tile.q) == null)
            {
                Vector3 worldPos = board.ResolveCoordinateToPosition(tile.p, tile.q);
                var soldierObj = gameplayManager.Runner.Spawn(ashSoldierPrefab, worldPos, Quaternion.identity, caster.Owner);
                var unit = soldierObj.GetComponent<NetworkUnit>();
                if (unit != null)
                {
                    unit.InitializeUnit(caster.Owner, "AshSoldier", 30, 3f, 1, 3, caster.Faction.ToString(), false);
                    unit.P = tile.p;
                    unit.Q = tile.q;
                }
                spawned++;
                if (spawned >= 4) break;
            }
        }
    }

    private List<HexTile> GetAdjacentTiles(BoardManager board, int p, int q)
    {
        List<HexTile> list = new List<HexTile>();
        int[,] dirs = new int[,] { { 1, 0 }, { 1, -1 }, { 0, -1 }, { -1, 0 }, { -1, 1 }, { 0, 1 } };
        for (int i = 0; i < 6; i++)
        {
            var tile = board.FindTile(p + dirs[i, 0], q + dirs[i, 1]);
            if (tile != null) list.Add(tile);
        }
        return list;
    }
}
