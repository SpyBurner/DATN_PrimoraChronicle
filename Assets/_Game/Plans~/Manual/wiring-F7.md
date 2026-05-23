# Wiring — F7 Behavior Registry

Legend: ⬜ todo · ✅ done

---

## Architecture: GDS vs SO responsibility

| Data | Source | Who reads it |
|---|---|---|
| `target_condition` (TargetMask) | GDS `SkillData.target_condition` | Client (targeting display) + Server (validation) |
| `target_pattern` / range | GDS `SkillData.target_pattern[0].n` | Client + Server |
| `display_pattern` / AOE | GDS `SkillData.display_pattern[0].n` | Client only |
| `cooldown`, `one_time` | GDS `SkillData.cooldown / one_time` | Server (slot setup) |
| `skill_behavior_id` | GDS `SkillData.skill_behavior_id` | Client (RPC) + Server (SO lookup) |
| **Execution logic** | `GenericSkillBehaviorSO` in `Resources/Behaviors/Skills/` | **Server only** |

**Clients never call `behavior.Execute()`.**  
`range`, `aoe`, `targetCondition` on `SkillBehaviorBaseSO` come from GDS — do **not** set them on SO assets (they are retained on the class for a future cleanup pass).

> Main phase spells are a GDS gap: `CardData` has no targeting fields for `MainPhaseSpell` type cards, so `range`/`aoe`/`targetCondition` **do** stay set on `MPS_*` SOs until GDS is extended.

---

## DI Bindings (`GameplayInstaller.cs`)

| Binding | Status |
|---|---|
| `BehaviorRegistryModel / Controller / Subsystem` | ✅ |

---

## Resources Folder Structure

| Folder | Status |
|---|---|
| `Assets/Resources/Behaviors/Skills/` | ✅ |
| `Assets/Resources/Behaviors/StatusEffects/` | ✅ |
| `Assets/Resources/Behaviors/MainPhaseSpells/` | ✅ |
| `Assets/Resources/Behaviors/Evolutions/` | ✅ |

---

## Skill Behavior Assets (`GenericSkillBehaviorSO`)

Right-click → **Create → Primora → Behaviors → GenericSkillBehavior**. Place in `Assets/Resources/Behaviors/Skills/`.

`range`, `aoe`, `targetCondition` come from GDS at runtime — leave at defaults on these assets.

| Asset name | `behaviorId` | `oneTime` | `cooldown` | Status |
|---|---|---|---|---|
| `SKB_CorruptedCrest` | `skb_corrupted_crest` | false | 2 | ✅ |
| `SKB_GraveclawFrenzy` | `skb_graveclaw_frenzy` | false | 2 | ✅ |
| `SKB_DeathsToll` | `skb_deaths_toll` | false | 2 | ✅ |
| `SKB_Cemetary` | `skb_cemetary` | false | 1 | ✅ |
| `SKB_Arise` | `skb_arise` | false | 2 | ✅ |
| `SKB_GroveheartsAscendance` | `skb_grovehearts_ascendance` | false | 2 | ✅ |
| `SKB_Sprout` | `skb_sprout` | false | 1 (Passive) | ✅ |
| `SKB_Bloom` | `skb_bloom` | false | 1 (Passive) | ✅ |
| `SKB_RootOvergrow` | `skb_root_overgrow` | false | 2 | ✅ |
| `SKB_DeepWoodsEntangle` | `skb_deep_woods_entangle` | false | 1 (Passive) | ✅ |
| `SKB_NaturesGift` | `skb_natures_gift` | false | 5 | ✅ |
| `SKB_LifeSappingThorn` | `skb_life_sapping_thorn` | false | 2 | ✅ |
| `SKB_WildGrowth` | `skb_wild_growth` | false | 2 | ✅ |
| `SKB_SporeBurst` | `skb_spore_burst` | false | 2 | ✅ |
| `SKB_BarkskinWard` | `skb_barkskin_ward` | false | 8 | ✅ |
| `SKB_SummonSeedling` | `skb_summon_seedling` | false | 6 | ✅ |
| `SKB_MasteryOfFlame` | `skb_mastery_of_flame` | false | 8 | ✅ |
| `SKB_SeveredTail` | `skb_severed_tail` | **true** | 0 | ✅ |
| `SKB_BannerOfCinders` | `skb_banner_of_cinders` | false | 15 | ✅ |
| `SKB_Firetrap` | `skb_firetrap` | false | 7 | ✅ |
| `SKB_MoltenDive` | `skb_molten_dive` | false | 5 | ✅ |
| `SKB_CurseOfAsh` | `skb_curse_of_ash` | false | 4 | ✅ |
| `SKB_LegionsLastStand` | `skb_legions_last_stand` | **true** | 0 | ✅ |
| `SKB_MarchOfEmbers` | `skb_march_of_embers` | false | 8 | ✅ |

### Skill `appliedStatusEffectId` and duration cross-reference

| Skill SO | `appliedStatusEffectId` | `appliedStatusDuration` |
|---|---|---|
| `SKB_RootOvergrow` | `seb_rooted` | 3 |
| `SKB_Arise` | `seb_decay` | −1 |
| `SKB_BannerOfCinders` | `seb_banner_of_cinders` | −1 |
| `SKB_Firetrap` | `seb_burning_trail` | 5 |
| `SKB_MoltenDive` | `seb_burning` | 1 |
| `SKB_BarkskinWard` | `seb_barkskin_ward` | −1 |
| `SKB_SeveredTail` | `seb_severed_tail` | 6 |
| `SKB_CurseOfAsh` | `seb_ash_cloud` | 7 |
| `SKB_MasteryOfFlame` | `seb_burning` (primary) | 3 |
| All others | _(none)_ | −1 |

> `behaviorId` must exactly match `skill_behavior_id` in GDS card data.

---

## Status Effect Assets (`GenericStatusEffectBehaviorSO`)

Right-click → **Create → Primora → Behaviors → GenericStatusEffectBehavior**. Place in `Assets/Resources/Behaviors/StatusEffects/`.

All `effectId` values use the `seb_` prefix to match GDS `status_effect_behavior_id`.

### Existing SOs (7) — updated

| Asset name | `effectId` | `damagePerTurn` | `interceptAmount` | Flags / Notes |
|---|---|---|---|---|
| `SE_Burning` | `seb_burning` | **1** | 0 | — |
| `SE_Melting` | `seb_melting` | **2** | 0 | — |
| `SE_BarkskinWard` | `seb_barkskin_ward` | 0 | 0 | Full-block next damage instance, consumed on trigger (`interceptAmount` unused) |
| `SE_Decay` | `seb_decay` | 0 | 0 | `preventsHealing=true` |
| `SE_Rooted` | `seb_rooted` | 0 | 0 | `preventsMovement=true` |
| `SE_BurningTrail` | `seb_burning_trail` | 0 | 0 | `leavesTrailOnMove=true`, `trailTileEffectId=seb_scorching_ground` |
| `SE_LegionsLastStand` _(was `SE_LegionsBuff`)_ | `seb_legions_last_stand` | 0 | 0 | Rename asset file to `SE_LegionsLastStand` |

### New SOs (13) — create

| Asset name | `effectId` | GDS Type | `maxStack` | Notes |
|---|---|---|---|---|
| `SE_Corrupted` | `seb_corrupted` | Tile | −1 | Deal 1 dmg + reduce N_Atk by 1 to enemies on tile each turn |
| `SE_Seeded` | `seb_seeded` | Tile | −1 | Allies on tile gain 1 Growth Stack at start of turn |
| `SE_GrowthStack` | `seb_growth_stack` | Unit | **4** | Tracks evolution stacks; triggers evolution at 4 |
| `SE_Ascendance` | `seb_ascendance` | Unit | **3** | Groveheart escalating buff |
| `SE_Entangled` | `seb_entangled` | Tile | −1 | Caps all skill/attack range to 1 hex for any unit on tile |
| `SE_ScorchingGround` | `seb_scorching_ground` | Tile | −1 | Applies Smoldering 2 turns; if already Smoldering → Burning 2 turns; if already Burning → reset duration |
| `SE_Smoldering` | `seb_smoldering` | Unit | −1 | Dormant heat; prerequisite for Burning; no direct damage |
| `SE_AshCloud` | `seb_ash_cloud` | Tile | −1 | Caps all skill/attack range to 1 hex (from tile) |
| `SE_SeveredTail` | `seb_severed_tail` | Tile | −1 | Deals 2 dmg to all units within 2-hex range at start of turn |
| `SE_BannerOfCinders` | `seb_banner_of_cinders` | Tile | −1 | All units in 2-hex range gain +2 to all damage |
| `SE_CursedDagger` | `seb_cursed_dagger` | Unit | **−1** (infinite) | +1 N_Atk permanently |
| `SE_CursedCloak` | `seb_cursed_cloak` | Unit | **−1** (infinite) | +5 MaxHP and current HP permanently |
| `SE_HumanityRejection` | `seb_humanity_rejection` | Unit | **−1** (infinite) | +2 N_Atk, −1 MaxHP/HP (Champion: HP reduction ignored) |

---

## Main Phase Spell Assets (`GenericMainPhaseSpellBehaviorSO`)

Right-click → **Create → Primora → Behaviors → GenericMainPhaseSpellBehavior**. Place in `Assets/Resources/Behaviors/MainPhaseSpells/`.

> **GDS gap**: `CardData` has no targeting fields for `MainPhaseSpell` type cards. `range`/`aoe`/`targetCondition` stay on these SOs until GDS is extended (unlike skill SOs where GDS supplies targeting).

| Asset name | `behaviorId` | `range` | `aoe` | `targetCondition` | Status |
|---|---|---|---|---|---|
| `MPS_CallOfDeath` | `mpsb_call_of_death` | 0 | 0 | 0 | ✅ |
| `MPS_BackToTheGrave` | `mpsb_back_to_the_grave` | 0 | 0 | 0 | ✅ |
| `MPS_Transplant` | `mpsb_transplant` | 99 | 0 | 4 | ✅ |

---

## Evolution Assets (`GenericEvolutionBehaviorSO`)

Right-click → **Create → Primora → Behaviors → GenericEvolution**. Place in `Assets/Resources/Behaviors/Evolutions/`.

`nextFormCardId` uses GDS `card_*` prefix. HP/Speed/MoveRange/DeathAnchor are **not** on these assets — read at runtime from `ICardLoadingManagerSubsystem.TryGetCardData(nextFormCardId)`.

| Asset name | `behaviorId` | `requiredStacks` | `nextFormCardId` | Status |
|---|---|---|---|---|
| `EVO_SeedlingToSapling` | `evo_seedling_sapling` | 4 | `card_sapling` | ✅ |
| `EVO_SaplingToYoungTreant` | `evo_sapling_young_treant` | 4 | `card_young_treant` | ✅ |
| `EVO_YoungTreantToThornColossus` | `evo_young_treant_thorn_colossus` | 4 | `card_thorn_colossus` | ✅ |

---

## Verify at Runtime

Enter Play Mode in the Gameplay scene. Console should print:
```
[BehaviorRegistry] Loaded 24 skills, 20 effects, 3 spells, 3 evolutions.
```
(20 effects = 7 updated existing + 13 new)

Any `Duplicate behaviorId` or `empty behaviorId` warning indicates a misconfigured asset.
