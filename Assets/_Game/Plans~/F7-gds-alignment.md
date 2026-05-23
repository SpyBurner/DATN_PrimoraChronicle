# F7 — GDS Alignment Plan

**Purpose**: Align `Manual/wiring-F7.md` and `F7-BehaviorRegistry-wiring-guide.md` with
the current GDS (`GDS/card.json` dated 2026-05-23) and correct the behavior SO architecture.

---

## Part 1 — Architecture Clarification

### Correct separation of concerns

| Data | Source | Who reads it |
|---|---|---|
| `target_condition` (TargetMask) | GDS `SkillData.target_condition` | Client (targeting display) + Server (validation) |
| `target_pattern` (valid tile ring) | GDS `SkillData.target_pattern[0].n` = range radius | Client + Server |
| `display_pattern` (AOE highlight ring) | GDS `SkillData.display_pattern[0].n` = AOE radius | Client only |
| `cooldown`, `one_time` | GDS `SkillData.cooldown / one_time` | Server (slot setup) |
| `skill_behavior_id` | GDS `SkillData.skill_behavior_id` | Client (sends ID in RPC) + Server (resolves SO) |
| **Execution logic** | `GenericSkillBehaviorSO` in `Resources/Behaviors/Skills/` | **Server only** |

**Clients never call `behavior.Execute()`.** The network flow is:
1. Client reads `SkillData` from `ICardLoadingManagerSubsystem.TryGetSkillData(skillId)` to build `TargetingRequest`
2. Client sends `(behaviorId, targetHexCoord)` via RPC
3. Server calls `BehaviorRegistrySubsystem.TryGetSkillBehavior(behaviorId).Execute(...)`

### What this means for the SO asset fields

`SkillBehaviorBaseSO` currently has `range`, `aoe`, `targetCondition`, `ignorePathfinding`,
`ignoreFriendlyFire`. These are **not needed for execution** and should not be set on assets:

- `range` / `aoe` / `targetCondition` → come from GDS; **do not set on the SO**
- `ignorePathfinding` / `ignoreFriendlyFire` → not yet in GDS; temporarily keep on SO
  (needed client-side in `TargetingRequest.IgnorePathfinding` until GDS is extended)

> **Follow-up code task (not this pass)**: Remove `range`, `aoe`, `targetCondition` fields
> from `SkillBehaviorBaseSO` and `MainPhaseSpellBehaviorBaseSO` base classes.
> Main phase spell targeting also has no GDS fields yet — those stay on the SO as a GDS gap.

---

## Part 2 — Status Effect SO Alignment

### 2a. Behavior ID prefix changed (`effectId` field)

All status effect `status_effect_behavior_id` values in GDS now use `seb_` prefix.
The `effectId` on every `GenericStatusEffectBehaviorSO` asset must match.

### 2b. Existing SOs — update required

| Old Asset Name | Old `effectId` | New `effectId` | Other changes |
|---|---|---|---|
| `SE_Burning` | `burning` | **`seb_burning`** | `damagePerTurn`: 10 → **1** |
| `SE_Melting` | `melting` | **`seb_melting`** | `damagePerTurn`: 20 → **2** |
| `SE_BarkskinWard` | `barkskin_ward` | **`seb_barkskin_ward`** | Behavior changed: was `interceptAmount: 15`, now **full-block next damage instance, consumed on trigger** (interceptAmount field unused) |
| `SE_Decay` | `decay` | **`seb_decay`** | No other changes |
| `SE_Rooted` | `rooted` | **`seb_rooted`** | No other changes |
| `SE_BurningTrail` | `burning_trail` | **`seb_burning_trail`** | `trailTileEffectId`: `"melting"` → **`"seb_scorching_ground"`** (GDS: trail leaves Scorching Ground, not Melting) |
| `SE_LegionsBuff` | `legions_buff` | **`seb_legions_last_stand`** | Rename asset to `SE_LegionsLastStand` |

### 2c. New SOs — create these assets

Right-click → **Create → Primora → Behaviors → GenericStatusEffectBehavior**.
Place all in `Assets/Resources/Behaviors/StatusEffects/`.

| Asset Name | `effectId` | GDS Type | Notes |
|---|---|---|---|
| `SE_Corrupted` | `seb_corrupted` | Tile | Deal 1 dmg + reduce N_Atk by 1 to enemies on tile each turn |
| `SE_Seeded` | `seb_seeded` | Tile | Allies on tile gain 1 Growth Stack at start of turn |
| `SE_GrowthStack` | `seb_growth_stack` | Unit | Tracks evolution stacks; `maxStack: 4`; triggers evolution at 4 |
| `SE_Ascendance` | `seb_ascendance` | Unit | Groveheart escalating buff; `maxStack: 3` |
| `SE_Entangled` | `seb_entangled` | Tile | Caps all skill/attack range to 1 hex for any unit on tile |
| `SE_ScorchingGround` | `seb_scorching_ground` | Tile | Applies Smoldering 2 turns; if already Smoldering → Burning 2 turns; if already Burning → reset duration |
| `SE_Smoldering` | `seb_smoldering` | Unit | Dormant heat; prerequisite for Burning; no direct damage |
| `SE_AshCloud` | `seb_ash_cloud` | Tile | Caps all skill/attack range to 1 hex (from tile) |
| `SE_SeveredTail` | `seb_severed_tail` | Tile | Deals 2 dmg to all units within 2-hex range at start of turn; `effectPattern n: 2` |
| `SE_BannerOfCinders` | `seb_banner_of_cinders` | Tile | All units in 2-hex range gain +2 to all damage; `effectPattern n: 2` |
| `SE_CursedDagger` | `seb_cursed_dagger` | Unit | +1 N_Atk permanently; `maxStack: -1` (infinite) |
| `SE_CursedCloak` | `seb_cursed_cloak` | Unit | +5 MaxHP and current HP permanently; `maxStack: -1` |
| `SE_HumanityRejection` | `seb_humanity_rejection` | Unit | +2 N_Atk, −1 MaxHP/HP (Champion: HP reduction ignored); `maxStack: -1` |

---

## Part 3 — Skill SO Alignment

### 3a. Remove targeting columns from wiring tables

`range`, `aoe`, `targetCondition` come from GDS `SkillData` at runtime. **Do not set these on
the SO assets.** Remove those three columns from the skills table in `Manual/wiring-F7.md`.

### 3b. Skill assets that need `appliedStatusEffectId` updated

Any skill SO that references a status effect ID must use the new `seb_` prefix:

| Skill SO | Old `appliedStatusEffectId` | New `appliedStatusEffectId` |
|---|---|---|
| `SKB_RootOvergrow` | `rooted` | `seb_rooted` |
| `SKB_Arise` | `decay` | `seb_decay` |
| `SKB_BannerOfCinders` | `banner_of_cinders` | `seb_banner_of_cinders` |
| `SKB_Firetrap` | `burning_trail` | `seb_burning_trail` |
| `SKB_MoltenDive` | `burning` | `seb_burning` |
| `SKB_BarkskinWard` | `barkskin_ward` | `seb_barkskin_ward` |

Any skill SO with a `tileEffectId` field also needs updating to `seb_` prefix.

### 3c. Skill asset `appliedStatusDuration` corrections

Cross-referencing GDS `SkillData.status_effects[*].duration`:

| Skill SO | Status Effect | GDS `duration` | Notes |
|---|---|---|---|
| `SKB_RootOvergrow` | `seb_rooted` | 3 | — |
| `SKB_Firetrap` | `seb_burning_trail` | 5 | — |
| `SKB_MoltenDive` | `seb_burning` | 1 | — |
| `SKB_SeveredTail` | `seb_severed_tail` | 6 | One-time skill |
| `SKB_CurseOfAsh` | `seb_ash_cloud` | 7 | — |
| `SKB_MasteryOfFlame` | `seb_burning` | 3 / `seb_melting` | dual-effect, see description |
| All others | — | `-1` | Permanent (no duration) |

### 3d. Skill assets: `oneTime` and `cooldown` from GDS

These values come from GDS but are also on the SO. Set them to match GDS exactly:

| Skill SO | `oneTime` | `cooldown` |
|---|---|---|
| `SKB_SeveredTail` | **true** | 0 |
| `SKB_LegionsLastStand` | **true** | 0 |
| `SKB_CorruptedCrest` | false | 2 |
| `SKB_GraveclawFrenzy` | false | 2 |
| `SKB_DeathsToll` | false | 2 |
| `SKB_Cemetary` | false | 1 |
| `SKB_Arise` | false | 2 |
| `SKB_GroveheartsAscendance` | false | 2 |
| `SKB_Sprout` | false | 1 (Passive) |
| `SKB_Bloom` | false | 1 (Passive) |
| `SKB_RootOvergrow` | false | 2 |
| `SKB_DeepWoodsEntangle` | false | 1 (Passive) |
| `SKB_NaturesGift` | false | 5 |
| `SKB_LifeSappingThorn` | false | 2 |
| `SKB_WildGrowth` | false | 2 |
| `SKB_SporeBurst` | false | 2 |
| `SKB_BarkskinWard` | false | 8 |
| `SKB_SummonSeedling` | false | 6 |
| `SKB_MasteryOfFlame` | false | 8 |
| `SKB_BannerOfCinders` | false | 15 |
| `SKB_Firetrap` | false | 7 |
| `SKB_MoltenDive` | false | 5 |
| `SKB_CurseOfAsh` | false | 4 |
| `SKB_MarchOfEmbers` | false | 8 |

---

## Part 4 — Evolution SO Alignment

The wiring guide lists `nextFormCardId` as `troop_sapling` etc., but GDS card `string_id` values use
`card_` prefix. The HP/Speed in the guide are wrong — those come from the card's GDS data, not the SO.

| Asset | `behaviorId` | `requiredStacks` | `nextFormCardId` (corrected) |
|---|---|---|---|
| `EVO_SeedlingToSapling` | `evo_seedling_sapling` | 4 | **`card_sapling`** |
| `EVO_SaplingToYoungTreant` | `evo_sapling_young_treant` | 4 | **`card_young_treant`** |
| `EVO_YoungTreantToThornColossus` | `evo_young_treant_thorn_colossus` | 4 | **`card_thorn_colossus`** |

Remove `nextFormHP`, `nextFormSpeed`, `nextFormMoveRange`, `nextFormDeathAnchor` from the
wiring table — these are read at runtime from `ICardLoadingManagerSubsystem.TryGetCardData(nextFormCardId)`.

---

## Part 5 — Main Phase Spell SOs

No ID changes needed (behavior IDs `mpsb_*` unchanged). GDS CardData has no targeting fields
for spells, so `range`/`aoe`/`targetCondition` stay on the SO as a GDS gap:

| Asset | `behaviorId` | `range` | `aoe` | `targetCondition` |
|---|---|---|---|---|
| `MPS_CallOfDeath` | `mpsb_call_of_death` | 0 | 0 | 0 |
| `MPS_BackToTheGrave` | `mpsb_back_to_the_grave` | 0 | 0 | 0 |
| `MPS_Transplant` | `mpsb_transplant` | 99 | 0 | 4 |

> **Note**: These values remain on the SO because GDS `CardData` has no targeting fields for
> `MainPhaseSpell` type cards. This is a known GDS gap. When GDS is extended with spell
> targeting data, these fields get removed from the SO (same pattern as skills).

---

## Part 6 — New Cards (no SO work, doc note only)

Three new hollow EquipSpell cards exist in GDS that were not in previous wiring docs.
They grant status effects on fuse and have no active skills — no new skill SO needed.

| Card | `string_id` | Grants |
|---|---|---|
| Cursed Dagger | `card_cursed_dagger` | `status_effect_cursed_dagger` → SO: `SE_CursedDagger` |
| Cursed Cloak | `card_cursed_cloak` | `status_effect_cursed_cloak` → SO: `SE_CursedCloak` |
| Humanity Rejection | `card_humanity_rejection` | `status_effect_humanity_rejection` → SO: `SE_HumanityRejection` |

---

## Step-by-Step Action Checklist

### Step 1 — Update `Manual/wiring-F7.md`
- [ ] Add architecture callout at top (GDS = targeting stats; SO = execution only)
- [ ] Skills table: remove `range`, `aoe`, `targetCondition` columns entirely
- [ ] Skills table: add `oneTime` and `cooldown` columns (values from Part 3d above)
- [ ] Status effects table: replace all `effectId` values with `seb_` prefix (Part 2b)
- [ ] Status effects table: add 13 new rows (Part 2c)
- [ ] Status effects table: add `damagePerTurn` correction note (Burning=1, Melting=2)
- [ ] Status effects table: update BarkskinWard note (full block, not -15)
- [ ] Status effects table: update BurningTrail note (trail=Scorching Ground)
- [ ] Evolution table: fix `nextFormCardId` values to `card_*` prefix
- [ ] Evolution table: remove HP/Speed/MoveRange/DeathAnchor columns
- [ ] Main phase spells section: add GDS-gap warning

### Step 2 — Update `F7-BehaviorRegistry-wiring-guide.md`
- [ ] Skills table: remove `range`, `aoe`, `targetCondition` columns
- [ ] Skills table: update "Field mapping" section to remove range/aoe/targetCondition
- [ ] Status effects table: update all effectIds to `seb_` prefix
- [ ] Status effects table: add all 13 new SOs
- [ ] Status effects table: correct Burning=1dmg, Melting=2dmg, BarkskinWard=full block
- [ ] Evolution table: fix `nextFormCardId`, remove stat columns

### Step 3 — Update existing SO assets (manual Editor work)
- [ ] Rename `SE_LegionsBuff` → `SE_LegionsLastStand`, set `effectId = seb_legions_last_stand`
- [ ] Update `effectId` on all 6 existing status effect SOs (Part 2b)
- [ ] Update `appliedStatusEffectId` on skill SOs that reference old effectIds (Part 3b)
- [ ] Create 13 new status effect SO assets (Part 2c)
- [ ] Fix `nextFormCardId` on 3 evolution SOs (Part 4)
- [ ] Set `oneTime=true` on `SKB_SeveredTail` and `SKB_LegionsLastStand`
- [ ] Verify all skill `cooldown` values match GDS (Part 3d)

### Step 4 — Verify in Play Mode
Enter Play Mode. Console should print:
```
[BehaviorRegistry] Loaded 24 skills, 20 effects, 3 spells, 3 evolutions.
```
(20 effects = 7 updated existing + 13 new)

Any `Duplicate behaviorId` or `empty behaviorId` warning indicates a misconfigured asset.

---

## Reference — GDS `target_pattern` Format

The `{n, p, q}` pattern entries encode ring radius + offset:
- `n: -1` = unlimited range (global — all tiles on board)
- `n: 0` = self tile only
- `n: X` (positive) = ring of radius X centered on caster/target

This maps directly to SO fields as:
- `target_pattern[0].n` ↔ SO `range`
- `display_pattern[0].n` ↔ SO `aoe`

Skills with `n: -1` in `target_pattern` are board-global (Spore Burst, Mastery of Flame,
March of Embers, Curse of Ash, Arise). The `TargetingSubsystem` should treat `Range = -1`
as "all tiles" when building the highlighted tile list.
