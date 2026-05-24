1. Match making needs rework => client freezes when not pressing match making at roughly the same time.
-> FIXED
2. No leaving mid-match

---

## BehaviorRegistry vs card.json audit (2026-05-24)

### ARCHITECTURE — Registry won't load these SOs
- `EvolutionBehaviorSO` (BehaviorRegistry/) extends `ScriptableObject` not `EvolutionBehaviorBaseSO` → invisible to `Resources.LoadAll<EvolutionBehaviorBaseSO>()`. Fix: change base class.
- `StatusEffectBehaviorSO` (BehaviorRegistry/) extends `ScriptableObject` not `StatusEffectBehaviorBaseSO` → same problem.

### MISSING LOGIC
- `mpsb_call_of_death`, `mpsb_back_to_the_grave`, `mpsb_transplant` — `GenericMainPhaseSpellBehaviorSO.Execute()` is a stub (only `Debug.Log`). No actual behavior.

### GenericCombatSkillBehaviorSO — GDS vs code mismatches
| Behavior | Bug |
|---|---|
| `skb_arise` | Spawns "Corrupted" tile effect (not in GDS). Should only apply Decay to unit. |
| `skb_legions_last_stand` | Applies status `"legions_buff"` — GDS says `"legions_last_stand"`. Wrong ID. |
| `skb_severed_tail` | Duration `9999` (GDS: 6); MaxHP loss = 10 (GDS: 6 HP = 60 at 10x scale). |
| `skb_banner_of_cinders` | Duration `4` (GDS: `-1` permanent). |
| `skb_mastery_of_flame` | Melting duration `9999` (GDS: `-1` permanent). |
| `skb_cemetary` + `skb_corrupted_crest` | Corrupted duration `3` (GDS: `-1` permanent). |
| `skb_graveclaw_frenzy` | `DealDamage(15)×2` hardcoded — GDS: "2 Normal Attacks", should use caster's `n_atk_dmg`. |
| `skb_deaths_toll` | `DealDamage(12)×2` hardcoded — same as above. |
| `skb_grovehearts_ascendance` | Ignores Ascendance level tiers. GDS: Lv1=growth stack only, Lv2=+heal, Lv3=+seed adjacent. Code does all 3 unconditionally. |
| `skb_spore_burst` (minor) | `DealDamage(15)` but GDS says 1 damage (= 10 at 10x scale). |