# F7 — BehaviorRegistry Wiring Guide

## Overview

The BehaviorRegistry subsystem loads all behavior ScriptableObjects from `Resources/` at initialization and provides typed lookups for the combat pipeline and UI panels.

---

## 1. Create Resource Folders

Create these folders under `Assets/Resources/`:

```
Assets/Resources/Behaviors/
├── Skills/          ← GenericSkillBehaviorSO assets
├── StatusEffects/   ← GenericStatusEffectBehaviorSO assets
├── MainPhaseSpells/ ← GenericMainPhaseSpellBehaviorSO assets
└── Evolutions/      ← GenericEvolutionBehaviorSO assets
```

---

## 2. Create Skill Behavior Assets

For each skill in the game, create a `GenericSkillBehaviorSO` asset:

**Right-click → Create → Primora → Behaviors → GenericSkillBehavior**

Place all assets in `Assets/Resources/Behaviors/Skills/`.

### Required skill assets (from LEGACY reference):

| Asset Name | behaviorId | range | aoe | targetCondition | Notes |
|---|---|---|---|---|---|
| `SKB_CorruptedCrest` | `skb_corrupted_crest` | 3 | 0 | 4 (EmptyTile) | Applies Corrupted tile effect |
| `SKB_GraveclawFrenzy` | `skb_graveclaw_frenzy` | 1 | 0 | 1 (Enemy) | 2× normal attack |
| `SKB_DeathsToll` | `skb_deaths_toll` | 2 | 2 | 1 (Enemy) | 2× normal attack AOE |
| `SKB_Cemetary` | `skb_cemetary` | 2 | 0 | 4 (EmptyTile) | Spawns Corrupted, duration=3 |
| `SKB_Arise` | `skb_arise` | 3 | 0 | 1 (Enemy) | Applies decay + corrupted on tile |
| `SKB_GroveheartsAscendance` | `skb_grovehearts_ascendance` | 3 | 0 | 4 (EmptyTile) | Seeded or enhance |
| `SKB_Sprout` | `skb_sprout` | 0 | 0 | 0 (Self) | +1 Growth Stack |
| `SKB_Bloom` | `skb_bloom` | 1 | 1 | 2 (Ally) | Heal 10 HP to allies in range |
| `SKB_RootOvergrow` | `skb_root_overgrow` | 1 | 0 | 1 (Enemy) | Applies rooted, duration=3 |
| `SKB_DeepWoodsEntangle` | `skb_deep_woods_entangle` | 1 | 1 | 1 (Enemy) | Entangled on tiles |
| `SKB_NaturesGift` | `skb_natures_gift` | 2 | 0 | 2 (Ally) | +1 Growth Stack to ally |
| `SKB_LifeSappingThorn` | `skb_life_sapping_thorn` | 1 | 0 | 1 (Enemy) | Attack + heal if on seeded |
| `SKB_WildGrowth` | `skb_wild_growth` | 2 | 0 | 4 (EmptyTile) | Seed or growth stack |
| `SKB_SporeBurst` | `skb_spore_burst` | 99 | 99 | 0 (Self) | 15 dmg to enemies on seeded tiles |
| `SKB_BarkskinWard` | `skb_barkskin_ward` | 2 | 0 | 2 (Ally) | Applies barkskin_ward |
| `SKB_SummonSeedling` | `skb_summon_seedling` | 1 | 0 | 0 (Self) | Summon persistent seedling |
| `SKB_MasteryOfFlame` | `skb_mastery_of_flame` | 2 | 1 | 1 (Enemy) | Burning or upgrade to Melting |
| `SKB_SeveredTail` | `skb_severed_tail` | 5 | 0 | 4 (EmptyTile) | 30 dmg, caster loses 10 MaxHP |
| `SKB_BannerOfCinders` | `skb_banner_of_cinders` | 0 | 0 | 0 (Self) | Tile effect on self tile |
| `SKB_Firetrap` | `skb_firetrap` | 0 | 0 | 0 (Self) | Applies burning_trail to self |
| `SKB_MoltenDive` | `skb_molten_dive` | 3 | 1 | 4 (EmptyTile) | Jump + AOE burning |
| `SKB_CurseOfAsh` | `skb_curse_of_ash` | 3 | 1 | 4 (EmptyTile) | AshCloud tile effect |
| `SKB_LegionsLastStand` | `skb_legions_last_stand` | 0 | 0 | 0 (Self) | Remove allies for buff |
| `SKB_MarchOfEmbers` | `skb_march_of_embers` | 0 | 0 | 0 (Self) | Summon 4 Ash Soldiers |

### Field mapping for each asset:

- **behaviorId**: Must match the `skill_behavior_id` from GDS `SkillData`
- **oneTime**: Set to `true` for skills marked `one_time` in GDS
- **cooldown**: Default `3` unless GDS specifies otherwise
- **range/aoe/targetCondition**: From table above
- **ignorePathfinding**: `true` for movement skills (MoltenDive)
- **ignoreFriendlyFire**: `true` for AOE that should hit allies too
- **summonPrefab**: Assign NetworkUnit prefab for summon skills (SummonSeedling, MarchOfEmbers)
- **tileEffectPrefab**: Assign TileEffectInstance prefab for tile-placing skills
- **tileEffectId**: e.g. "Corrupted", "Seeded", "AshCloud", "BannerOfCinders"
- **tileEffectDuration**: Per skill (usually 3-7)
- **directDamage/directHeal**: For simple dmg/heal skills
- **appliedStatusEffectId/appliedStatusDuration**: For status-applying skills
- **growthStacksGranted**: For Nature faction growth skills

---

## 3. Create Status Effect Behavior Assets

**Right-click → Create → Primora → Behaviors → GenericStatusEffectBehavior**

Place in `Assets/Resources/Behaviors/StatusEffects/`.

| Asset Name | effectId | damagePerTurn | interceptAmount | Flags | Notes |
|---|---|---|---|---|---|
| `SE_Burning` | `burning` | 10 | 0 | — | Tick damage |
| `SE_Melting` | `melting` | 20 | 0 | — | Upgraded burning |
| `SE_BarkskinWard` | `barkskin_ward` | 0 | 15 | — | Intercepts 15 per hit |
| `SE_Decay` | `decay` | 0 | 0 | preventsHealing=true | Blocks all healing |
| `SE_Rooted` | `rooted` | 0 | 0 | preventsMovement=true | Cannot move |
| `SE_BurningTrail` | `burning_trail` | 0 | 0 | leavesTrailOnMove=true | trailTileEffectId="Melting" |
| `SE_LegionsBuff` | `legions_buff` | 0 | 0 | — | Offensive buff (handled in combat) |

---

## 4. Create Main Phase Spell Behavior Assets

**Right-click → Create → Primora → Behaviors → GenericMainPhaseSpellBehavior**

Place in `Assets/Resources/Behaviors/MainPhaseSpells/`.

Create one asset per unique `main_phase_spell_behavior_id` from GDS card data. Wire:
- **behaviorId**: matches `CardData.main_phase_spell_behavior_id`
- **range/aoe/targetCondition**: from the spell card's GDS data
- **appliedTileEffectId/tileEffectDuration**: if the spell places a tile effect
- **appliedStatusEffectId/statusEffectDuration**: if the spell applies a status
- **directDamage/directHeal**: if the spell does immediate damage/heal

---

## 5. Create Evolution Behavior Assets

**Right-click → Create → Primora → Behaviors → GenericEvolutionBehavior**

Place in `Assets/Resources/Behaviors/Evolutions/`.

| Asset Name | behaviorId | requiredStacks | nextFormCardId | HP | Speed | MoveRange | DeathAnchor |
|---|---|---|---|---|---|---|---|
| `EVO_SeedlingToSapling` | `evo_seedling_sapling` | 4 | `troop_sapling` | 50 | 2.5 | 2 | 2 |
| `EVO_SaplingToYoungTreant` | `evo_sapling_young_treant` | 4 | `troop_young_treant` | 65 | 3 | 3 | 3 |
| `EVO_YoungTreantToThornColossus` | `evo_young_treant_thorn_colossus` | 4 | `troop_thorn_colossus` | 80 | 3.5 | 3 | 5 |

- **nextFormPrefab**: Assign the visual prefab for the evolved unit form

---

## 6. Verify Loading

After creating all assets, enter Play Mode in the Gameplay scene. Check the console for:

```
[BehaviorRegistry] Loaded X skills, Y effects, Z spells, W evolutions.
```

Any warnings like:
```
[BehaviorRegistry] SkillBehavior asset 'XXX' has empty behaviorId — skipped.
[BehaviorRegistry] Duplicate skill behaviorId 'XXX' — skipped 'YYY'.
```
indicate misconfigured assets that need fixing.

---

## 7. File Summary

### New files created (Core.Interfaces):
| File | Purpose |
|---|---|
| `Core/Scripts/Interfaces/Features/Gameplay/BehaviorRegistry/SkillBehaviorBaseSO.cs` | Abstract base for skill behaviors (data fields) |
| `Core/Scripts/Interfaces/Features/Gameplay/BehaviorRegistry/StatusEffectBehaviorBaseSO.cs` | Abstract base for status effects |
| `Core/Scripts/Interfaces/Features/Gameplay/BehaviorRegistry/MainPhaseSpellBehaviorBaseSO.cs` | Abstract base for main phase spells |
| `Core/Scripts/Interfaces/Features/Gameplay/BehaviorRegistry/EvolutionBehaviorBaseSO.cs` | Abstract base for evolution chain |
| `Core/Scripts/Interfaces/Features/Gameplay/BehaviorRegistry/SkillExecutionContext.cs` | Execution context struct |

### New files created (GameplayFeatures):
| File | Purpose |
|---|---|
| `Features/Gameplay/Scripts/ScriptableObjects/GenericSkillBehaviorSO.cs` | Concrete skill behavior with Execute + CreateAssetMenu |
| `Features/Gameplay/Scripts/ScriptableObjects/GenericStatusEffectBehaviorSO.cs` | Concrete status effect with tick/intercept |
| `Features/Gameplay/Scripts/ScriptableObjects/GenericMainPhaseSpellBehaviorSO.cs` | Concrete main phase spell |
| `Features/Gameplay/Scripts/ScriptableObjects/GenericEvolutionBehaviorSO.cs` | Concrete evolution behavior |

### Modified files:
| File | Change |
|---|---|
| `Core/Scripts/Interfaces/.../IBehaviorRegistrySubsystem.cs` | Returns strongly-typed SO bases + `LoadAll()` + `TryGetEvolutionBehavior()` |
| `Core/Scripts/Interfaces/.../IBehaviorRegistryModel.cs` | Strongly-typed dictionaries + evolution dictionary |
| `Core/Scripts/Interfaces/.../IBehaviorRegistryController.cs` | Strongly-typed + `LoadAll()` + evolution lookup |
| `Features/Gameplay/Scripts/BehaviorRegistry/BehaviorRegistryModel.cs` | Strongly-typed dictionaries |
| `Features/Gameplay/Scripts/BehaviorRegistry/BehaviorRegistryController.cs` | `Resources.LoadAll` from 4 folders, validation logging |
| `Features/Gameplay/Scripts/BehaviorRegistry/BehaviorRegistrySubsystem.cs` | Calls `LoadAll()` on Initialize, try/catch |

---

## 8. No Prefab Wiring Required

The BehaviorRegistry subsystem is DI-bound via `GameplayInstaller` (already done). It has no MonoBehaviour component and no prefab. It loads assets purely from `Resources/` folders at runtime.

The only manual work is **creating the ScriptableObject assets** in the folders listed above.
