# Manual Unity Editor Wiring — F7 BehaviorRegistry

**Scope:** Everything needed to populate and verify `BehaviorRegistrySubsystem` (F7.1) in the Editor. The subsystem loads `GenericSkillBehaviorSO`, `GenericStatusEffectBehaviorSO`, `GenericMainPhaseSpellBehaviorSO`, and `GenericEvolutionBehaviorSO` from `Resources/` at Gameplay scene initialization.
**Prerequisites:** F1 wiring complete (GameplayCoordinator prefab in scene, GameplayInstaller active).

---

## Legend

| Symbol | Meaning |
|---|---|
| ⬜ | Not yet wired |
| ✅ | Wired and verified |
| 🔨 | Requires a new asset to be created first |

---

## F7 — DI Bindings (GameplayInstaller)

Already present in `GameplayInstaller.cs`. No action required.

| Binding | Status |
|---|---|
| `BehaviorRegistryModel` / `BehaviorRegistryController` / `BehaviorRegistrySubsystem` | ✅ |

---

## F7 — Resources Folder Structure

The `BehaviorRegistryController.LoadAll()` calls `Resources.LoadAll<T>(path)` for each of the four paths below. The folders **must exist** under `Assets/Resources/`; Unity will silently return empty arrays if a folder is missing.

```
Assets/Resources/Behaviors/
├── Skills/          ← GenericSkillBehaviorSO assets
├── StatusEffects/   ← GenericStatusEffectBehaviorSO assets
├── MainPhaseSpells/ ← GenericMainPhaseSpellBehaviorSO assets
└── Evolutions/      ← GenericEvolutionBehaviorSO assets
```

| Step | Status |
|---|---|
| Create `Assets/Resources/Behaviors/Skills/` | ⬜ |
| Create `Assets/Resources/Behaviors/StatusEffects/` | ⬜ |
| Create `Assets/Resources/Behaviors/MainPhaseSpells/` | ⬜ |
| Create `Assets/Resources/Behaviors/Evolutions/` | ⬜ |

---

## F7 — ScriptableObject Base Classes (Core.Interfaces)

These abstract classes were introduced by F7 and live in `Core/Scripts/Interfaces/Features/Gameplay/BehaviorRegistry/`. They are the types returned by `IBehaviorRegistrySubsystem.TryGet*`. Existing concrete SO classes now inherit from them:

| Abstract Base | Existing concrete that inherits it |
|---|---|
| `SkillBehaviorBaseSO` | `CombatSkillBehaviorSO` (via merge fix), `GenericSkillBehaviorSO` |
| `StatusEffectBehaviorBaseSO` | `GenericStatusEffectBehaviorSO` |
| `MainPhaseSpellBehaviorBaseSO` | `MainPhaseSpellBehaviorSO` (via merge fix), `GenericMainPhaseSpellBehaviorSO` |
| `EvolutionBehaviorBaseSO` | `GenericEvolutionBehaviorSO` |

No Inspector action required — inheritance is code-only.

---

## F7 — Skill Behavior Assets

### 🔨 Create `GenericSkillBehaviorSO` assets

**Right-click → Create → Primora → Behaviors → GenericSkillBehavior**

Place all assets in `Assets/Resources/Behaviors/Skills/`.

| Asset Name | behaviorId | range | aoe | targetCondition |
|---|---|---|---|---|
| `SKB_CorruptedCrest` | `skb_corrupted_crest` | 3 | 0 | 4 |
| `SKB_GraveclawFrenzy` | `skb_graveclaw_frenzy` | 1 | 0 | 1 |
| `SKB_DeathsToll` | `skb_deaths_toll` | 2 | 2 | 1 |
| `SKB_Cemetary` | `skb_cemetary` | 2 | 0 | 4 |
| `SKB_Arise` | `skb_arise` | 3 | 0 | 1 |
| `SKB_GroveheartsAscendance` | `skb_grovehearts_ascendance` | 3 | 0 | 4 |
| `SKB_Sprout` | `skb_sprout` | 0 | 0 | 0 |
| `SKB_Bloom` | `skb_bloom` | 1 | 1 | 2 |
| `SKB_RootOvergrow` | `skb_root_overgrow` | 1 | 0 | 1 |
| `SKB_DeepWoodsEntangle` | `skb_deep_woods_entangle` | 1 | 1 | 1 |
| `SKB_NaturesGift` | `skb_natures_gift` | 2 | 0 | 2 |
| `SKB_LifeSappingThorn` | `skb_life_sapping_thorn` | 1 | 0 | 1 |
| `SKB_WildGrowth` | `skb_wild_growth` | 2 | 0 | 4 |
| `SKB_SporeBurst` | `skb_spore_burst` | 99 | 99 | 0 |
| `SKB_BarkskinWard` | `skb_barkskin_ward` | 2 | 0 | 2 |
| `SKB_SummonSeedling` | `skb_summon_seedling` | 1 | 0 | 0 |
| `SKB_MasteryOfFlame` | `skb_mastery_of_flame` | 2 | 1 | 1 |
| `SKB_SeveredTail` | `skb_severed_tail` | 5 | 0 | 4 |
| `SKB_BannerOfCinders` | `skb_banner_of_cinders` | 0 | 0 | 0 |
| `SKB_Firetrap` | `skb_firetrap` | 0 | 0 | 0 |
| `SKB_MoltenDive` | `skb_molten_dive` | 3 | 1 | 4 |
| `SKB_CurseOfAsh` | `skb_curse_of_ash` | 3 | 1 | 4 |
| `SKB_LegionsLastStand` | `skb_legions_last_stand` | 0 | 0 | 0 |
| `SKB_MarchOfEmbers` | `skb_march_of_embers` | 0 | 0 | 0 |

Status: ⬜ All skill assets created

> **behaviorId** must exactly match the `skill_behavior_id` string in GDS `SkillData`. A missing or mistyped ID means the skill silently does nothing in combat.

---

## F7 — Status Effect Behavior Assets

### 🔨 Create `GenericStatusEffectBehaviorSO` assets

**Right-click → Create → Primora → Behaviors → GenericStatusEffectBehavior**

Place in `Assets/Resources/Behaviors/StatusEffects/`.

| Asset Name | effectId | damagePerTurn | interceptAmount | Flags |
|---|---|---|---|---|
| `SE_Burning` | `burning` | 10 | 0 | — |
| `SE_Melting` | `melting` | 20 | 0 | — |
| `SE_BarkskinWard` | `barkskin_ward` | 0 | 15 | — |
| `SE_Decay` | `decay` | 0 | 0 | preventsHealing=true |
| `SE_Rooted` | `rooted` | 0 | 0 | preventsMovement=true |
| `SE_BurningTrail` | `burning_trail` | 0 | 0 | leavesTrailOnMove=true, trailTileEffectId=`melting` |
| `SE_LegionsBuff` | `legions_buff` | 0 | 0 | — |

Status: ⬜ All status effect assets created

> **effectId** must match the string passed to `NetworkUnit.ServerAddStatus()`. The damage pipeline's `InterceptWithStatusEffects` pass looks up the `effectId` to apply intercept.

---

## F7 — Main Phase Spell Behavior Assets

### 🔨 Create `GenericMainPhaseSpellBehaviorSO` assets

**Right-click → Create → Primora → Behaviors → GenericMainPhaseSpellBehavior**

Place in `Assets/Resources/Behaviors/MainPhaseSpells/`.

Create one asset per unique `main_phase_spell_behavior_id` from GDS card data. Minimum fields per asset:

| Field | Source |
|---|---|
| `behaviorId` | Must match `CardData.main_phase_spell_behavior_id` |
| `range` / `aoe` / `targetCondition` | From the spell card's GDS data |

Status: ⬜ Main phase spell assets created

---

## F7 — Evolution Behavior Assets

### 🔨 Create `GenericEvolutionBehaviorSO` assets

**Right-click → Create → Primora → Behaviors → GenericEvolution**

Place in `Assets/Resources/Behaviors/Evolutions/`.

| Asset Name | behaviorId | requiredStacks | nextFormCardId |
|---|---|---|---|
| `EVO_SeedlingToSapling` | `evo_seedling_sapling` | 4 | `troop_sapling` |
| `EVO_SaplingToYoungTreant` | `evo_sapling_young_treant` | 4 | `troop_young_treant` |
| `EVO_YoungTreantToThornColossus` | `evo_young_treant_thorn_colossus` | 4 | `troop_thorn_colossus` |

Status: ⬜ Evolution assets created

---

## F7 — Verify Loading at Runtime

Enter Play Mode in the Gameplay scene. Check the console for the load summary line:

```
[BehaviorRegistry] Loaded X skills, Y effects, Z spells, W evolutions.
```

Expected counts once all assets are created: 24 skills, 7 effects, N spells, 3 evolutions.

Any missing assets appear as warnings:
```
[BehaviorRegistry] SkillBehavior asset 'XXX' has empty behaviorId — skipped.
[BehaviorRegistry] Duplicate skill behaviorId 'XXX' — skipped 'YYY'.
```

| Verify | Status |
|---|---|
| Console shows `[BehaviorRegistry] Loaded X skills...` on Play | ⬜ |
| No empty-behaviorId warnings for skill / effect assets | ⬜ |
| `CombatNetworkView` resolves skill behaviors without null warnings | ⬜ |
| `PlayerCardZoneNetworkView` resolves spell behaviors without null warnings | ⬜ |

---

## F7 — No Prefab Wiring Required

`BehaviorRegistrySubsystem` is DI-bound via `GameplayInstaller` (already in place). It has no `MonoBehaviour`, no NetworkObject prefab, and no `NetworkViewRegistry` entry. Asset loading is fully automatic at Gameplay scene load via `Initialize() → LoadAll()`.

---

## F7 — Post-Merge Fixes Applied (reference only)

These fixes were made during the F7 merge to resolve compile errors introduced by the new base classes. No further action needed.

| Fix | Detail |
|---|---|
| `CombatSkillBehaviorSO` base class | Changed from `ScriptableObject` to `SkillBehaviorBaseSO`; duplicate field declarations removed |
| `MainPhaseSpellBehaviorSO` base class | Changed from `ScriptableObject` to `MainPhaseSpellBehaviorBaseSO`; duplicate `behaviorId` field removed |
| `SkillExecutionContext` rename | Old struct in `Combat/SkillExecutionContext.cs` renamed to `CombatSkillExecutionContext` to eliminate CS0436 conflict with F7's `Core.Interfaces` version |
| `HexTile.cs` moved | Moved from `LEGACY/_NetworkMono/` to `Board/` — the new `GameplayFeatures.LEGACY.asmdef` had isolated it from `GameplayFeatures`, breaking `BoardNetworkView` |
