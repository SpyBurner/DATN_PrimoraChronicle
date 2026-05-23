# Wiring — F7 Behavior Registry

Legend: ⬜ todo · ✅ done

---

## DI Bindings (`GameplayInstaller.cs`)

| Binding | Status |
|---|---|
| `BehaviorRegistryModel / Controller / Subsystem` | ✅ |

---

## Resources Folder Structure

Create these folders. Unity silently returns empty arrays if any folder is missing.

| Folder | Status |
|---|---|
| `Assets/Resources/Behaviors/Skills/` | ⬜ |
| `Assets/Resources/Behaviors/StatusEffects/` | ⬜ |
| `Assets/Resources/Behaviors/MainPhaseSpells/` | ⬜ |
| `Assets/Resources/Behaviors/Evolutions/` | ⬜ |

---

## Skill Behavior Assets (`GenericSkillBehaviorSO`)

Right-click → **Create → Primora → Behaviors → GenericSkillBehavior**. Place in `Assets/Resources/Behaviors/Skills/`.

| Asset name | `behaviorId` | range | aoe | targetCondition | Status |
|---|---|---|---|---|---|
| `SKB_CorruptedCrest` | `skb_corrupted_crest` | 3 | 0 | 4 | ⬜ |
| `SKB_GraveclawFrenzy` | `skb_graveclaw_frenzy` | 1 | 0 | 1 | ⬜ |
| `SKB_DeathsToll` | `skb_deaths_toll` | 2 | 2 | 1 | ⬜ |
| `SKB_Cemetary` | `skb_cemetary` | 2 | 0 | 4 | ⬜ |
| `SKB_Arise` | `skb_arise` | 3 | 0 | 1 | ⬜ |
| `SKB_GroveheartsAscendance` | `skb_grovehearts_ascendance` | 3 | 0 | 4 | ⬜ |
| `SKB_Sprout` | `skb_sprout` | 0 | 0 | 0 | ⬜ |
| `SKB_Bloom` | `skb_bloom` | 1 | 1 | 2 | ⬜ |
| `SKB_RootOvergrow` | `skb_root_overgrow` | 1 | 0 | 1 | ⬜ |
| `SKB_DeepWoodsEntangle` | `skb_deep_woods_entangle` | 1 | 1 | 1 | ⬜ |
| `SKB_NaturesGift` | `skb_natures_gift` | 2 | 0 | 2 | ⬜ |
| `SKB_LifeSappingThorn` | `skb_life_sapping_thorn` | 1 | 0 | 1 | ⬜ |
| `SKB_WildGrowth` | `skb_wild_growth` | 2 | 0 | 4 | ⬜ |
| `SKB_SporeBurst` | `skb_spore_burst` | 99 | 99 | 0 | ⬜ |
| `SKB_BarkskinWard` | `skb_barkskin_ward` | 2 | 0 | 2 | ⬜ |
| `SKB_SummonSeedling` | `skb_summon_seedling` | 1 | 0 | 0 | ⬜ |
| `SKB_MasteryOfFlame` | `skb_mastery_of_flame` | 2 | 1 | 1 | ⬜ |
| `SKB_SeveredTail` | `skb_severed_tail` | 5 | 0 | 4 | ⬜ |
| `SKB_BannerOfCinders` | `skb_banner_of_cinders` | 0 | 0 | 0 | ⬜ |
| `SKB_Firetrap` | `skb_firetrap` | 0 | 0 | 0 | ⬜ |
| `SKB_MoltenDive` | `skb_molten_dive` | 3 | 1 | 4 | ⬜ |
| `SKB_CurseOfAsh` | `skb_curse_of_ash` | 3 | 1 | 4 | ⬜ |
| `SKB_LegionsLastStand` | `skb_legions_last_stand` | 0 | 0 | 0 | ⬜ |
| `SKB_MarchOfEmbers` | `skb_march_of_embers` | 0 | 0 | 0 | ⬜ |

> `behaviorId` must exactly match `skill_behavior_id` in GDS card data.

---

## Status Effect Assets (`GenericStatusEffectBehaviorSO`)

Right-click → **Create → Primora → Behaviors → GenericStatusEffectBehavior**. Place in `Assets/Resources/Behaviors/StatusEffects/`.

| Asset name | `effectId` | damagePerTurn | interceptAmount | Flags | Status |
|---|---|---|---|---|---|
| `SE_Burning` | `burning` | 10 | 0 | — | ⬜ |
| `SE_Melting` | `melting` | 20 | 0 | — | ⬜ |
| `SE_BarkskinWard` | `barkskin_ward` | 0 | 15 | — | ⬜ |
| `SE_Decay` | `decay` | 0 | 0 | preventsHealing=true | ⬜ |
| `SE_Rooted` | `rooted` | 0 | 0 | preventsMovement=true | ⬜ |
| `SE_BurningTrail` | `burning_trail` | 0 | 0 | leavesTrailOnMove=true, trailTileEffectId=`melting` | ⬜ |
| `SE_LegionsBuff` | `legions_buff` | 0 | 0 | — | ⬜ |

---

## Main Phase Spell Assets (`GenericMainPhaseSpellBehaviorSO`)

Right-click → **Create → Primora → Behaviors → GenericMainPhaseSpellBehavior**. Place in `Assets/Resources/Behaviors/MainPhaseSpells/`.

One asset per unique `main_phase_spell_behavior_id` found in GDS card data.

| Field | Source |
|---|---|
| `behaviorId` | Must match `CardData.main_phase_spell_behavior_id` exactly |
| `range` / `aoe` / `targetCondition` | From the spell card's GDS entry |

Status: ⬜ (create one per spell behavior ID from GDS)

---

## Evolution Assets (`GenericEvolutionBehaviorSO`)

Right-click → **Create → Primora → Behaviors → GenericEvolution**. Place in `Assets/Resources/Behaviors/Evolutions/`.

| Asset name | `behaviorId` | `requiredStacks` | `nextFormCardId` | Status |
|---|---|---|---|---|
| `EVO_SeedlingToSapling` | `evo_seedling_sapling` | 4 | `troop_sapling` | ⬜ |
| `EVO_SaplingToYoungTreant` | `evo_sapling_young_treant` | 4 | `troop_young_treant` | ⬜ |
| `EVO_YoungTreantToThornColossus` | `evo_young_treant_thorn_colossus` | 4 | `troop_thorn_colossus` | ⬜ |

---

## Verify at Runtime

Enter Play Mode in the Gameplay scene. Console should print:
```
[BehaviorRegistry] Loaded X skills, Y effects, Z spells, W evolutions.
```
Expected counts once all assets exist: **24 skills, 7 effects, 3 evolutions** (+ N main phase spells from GDS).
