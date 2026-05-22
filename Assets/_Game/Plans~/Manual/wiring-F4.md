# Manual Unity Editor Wiring — F4 Combat Phase

**Scope:** Everything needed to run F4.1–F4.15 in the Editor (action queue, turn order panel,
skill panel, targeting overlay, damage pipeline, status/tile effects, death + DeathAnchor,
persistent units, Verdant evolution, board clear).

**Prerequisite:** F1 + F2 + F3 wiring complete (SceneContext, GameplayInstaller, all prior prefabs registered).

---

## Legend

| Symbol | Meaning |
|---|---|
| ⬜ | Not yet wired |
| ✅ | Already done in code / no Editor action needed |
| 🔨 | Requires a new prefab to be created first |

---

## Compile Status (as of audit 2026-05-23)

**2 compile errors — project does NOT compile clean.**

| Error | File | Line | Root Cause |
|---|---|---|---|
| CS0246 `CombatNetworkView` not found | `GameplayNetworkCoordinator.cs` | 33 | Unity incremental-compiler stale state after merge — `CombatNetworkView` class exists in `Combat/CombatNetworkView.cs` (same `GameplayFeatures` assembly) but Unity has not re-emitted it. Fix: **Assets → Reimport All** or delete `Library/ScriptAssemblies` and let Unity rebuild. |
| CS0246 `CombatNetworkView` not found | `GameplayNetworkCoordinator.cs` | 251 | Same root cause as above. |

> **Do NOT wire anything in this file until the compile errors above are fixed.**

---

## Critical API Mismatches (will surface as additional compile errors once stale state clears)

| # | Severity | File | Issue |
|---|---|---|---|
| 1 | **Compile error** | `SkillPanel.cs:53,54,74,220` | `ICombatSubsystem` has `CurrentTurnChanged` typed `UnityAction<NetworkId>` and `CurrentActor` typed `NetworkId`; `SkillPanel` calls them as `string`. `IsCombatActive` and `CurrentActorId` do not exist on `ICombatSubsystem`. `RequestEndTurn()` does not exist — interface exposes `EndTurn()`. |
| 2 | **Compile error** | `TurnOrderPanel.cs:31,34,35,54,63,104,114` | Same: `CurrentTurnChanged` handler typed `string`; `IsCombatActive`, `CurrentActorId`, `ActionQueue` typed as `IReadOnlyList<string>` but interface returns `IReadOnlyList<CombatQueueEntry>`. |
| 3 | **Compile error** | `CombatSubsystem.cs:13,47` | `QueueChanged` is declared `UnityAction<IReadOnlyList<CombatQueueEntry>>` in `CombatSubsystem` but `ICombatSubsystem` also declares it the same way — **consistent**. However `TurnOrderPanel` expects `IReadOnlyList<string>`, causing a handler signature mismatch. |
| 4 | **Compile error** | `CombatNetworkView.cs` (PushState, line 860) | `CombatStateData.CurrentActorId` is a `string` field but `ICombatSubsystem.CurrentActor` / `CombatModel._currentActor` is typed `Observable<NetworkId>` and `CombatStateData.CurrentActor` is `NetworkId`. The `PushState()` method constructs `CombatStateData { CurrentActorId = currentActor }` — field name mismatch; struct has `CurrentActor` (`NetworkId`), not `CurrentActorId` (`string`). |

> The above bugs are **blocked by the stale compile state** — they will become visible errors only after Reimport All.

---

## F4.1 — Action Queue Build

### Spec Verification

| Spec requirement | Status | Notes |
|---|---|---|
| `BuildQueue()` sorts Speed desc → HP asc → coin toss | ✅ | `CombatNetworkView.BuildActionQueue()` uses `Runner.Tick` seeded RNG; sort comparator is correct |
| Mid-combat spawns appended via `AppendToQueue(unitId)` | ✅ | `ServerAppendToQueue(string unitId)` exists; cap=20 checked |
| Queue stored as `NetworkArray<NetworkString<_32>>` capacity 20 | ✅ | Networked props declared correctly |

---

## F4.2 — TurnOrder Panel

### 🔨 Create `PhaseInteractionPanel_TurnOrder.prefab`

| Property | Value |
|---|---|
| Components | `RectTransform` + `TurnOrderPanel` MonoBehaviour |
| Parent | `TurnOrderPanelAnchor` → `PanelDrawer._panel` reference |

> **Fix required first** — `TurnOrderPanel.cs` has API mismatches with `ICombatSubsystem` (see Critical API Mismatches #2 above).

| Field on `TurnOrderPanel` | Assign | Status |
|---|---|---|
| `_content` | `Content` Transform child (vertical layout group) | ⬜ |
| `_turnOrderItemPrefab` | `TurnOrderItem.prefab` (has `Image` + `TMP_Text`) | ⬜ |

> Injection (`ICombatSubsystem`, `IUnitSubsystem`, `IGameStateSubsystem`, `INetworkManagerSubsystem`,
> `ICardLoadingManagerSubsystem`) via Zenject — no Inspector assignment.

### Wire `TurnOrderPanelAnchor.prefab` (PanelDrawer)

| Field on `PanelDrawer` | Assign | Status |
|---|---|---|
| `_panel` | `PhaseInteractionPanel_TurnOrder` RectTransform | ⬜ |
| `_toggle` | `Toggle_Sidebar` Toggle component on the anchor | ⬜ |

### Spec Verification

| Spec requirement | Status | Notes |
|---|---|---|
| Subscribes `ICombatSubsystem.QueueChanged` | ✅ | Code subscribes in `OnEnable` |
| Each `CombatQueueEntry` carries UnitId + CardId for card image | ⬜ **PARTIAL** | `TurnOrderPanel` renders only a name label; no card image `Image` component or sprite lookup in the item prefab. Card art display is not implemented. |
| Drawer-wrapped by `TurnOrderPanelAnchor` | ⬜ | Prefab not created yet |

---

## F4.3 — Unit Turn Cycle

### Spec Verification

| Spec requirement | Status | Notes |
|---|---|---|
| `AdvanceTurn()` resets HasMoved=false, HasActed=false on enter | ✅ | `StartCurrentActorTurn()` sets `CurrentActorHasMoved = HasStatus(data,"rooted")` and `CurrentActorHasActed = false` |
| Rooted unit: HasMoved pre-set true | ✅ | `CurrentActorHasMoved = HasStatus(data,"rooted")` |
| Tick all 6 skill CDs on turn start | ✅ | `unitView.ServerTickCooldowns()` called in `StartCurrentActorTurn` |
| After RequestMove: HasMoved=true | ✅ | `CurrentActorHasMoved = true` set in `ServerMove()` |
| After RequestNormalAttack/Skill: HasActed=true | ✅ | `CurrentActorHasActed = true` set in both `ServerNormalAttack` and `ServerSkill` |
| EndTurn() valid at any point | ✅ | `Rpc_RequestEndTurn` → `ServerEndTurn()`, no HasMoved/HasActed guard |
| Auto-end on no-input timer | ✅ | `FixedUpdateNetwork` checks `TurnTimer.Expired()` → `ServerEndTurn()` |

---

## F4.4 — Movement & Pathfinding

### Spec Verification

| Spec requirement | Status | Notes |
|---|---|---|
| `BoardSubsystem.FindPath(from, to)` walks empty tiles | ✅ | Called in `ServerMove` with `data.MoveRange` |
| Max distance=1 (adjacent hex) | ⬜ **UNVERIFIED** | `ServerMove` passes `data.MoveRange` to `FindPath`; spec says max=1 but `data.MoveRange` could be any value. The MoveRange field in `UnitStateData` needs to be verified to default to 1 for all units. |
| Knockback stops at board boundary | ⬜ **UNVERIFIED** | No knockback implementation visible in `CombatNetworkView`; must be in a `CombatSkillBehaviorSO` behavior (e.g., `push`). Board boundary clamping not audited. |

---

## F4.5 — Skill Panel + Active Skill Use

### 🔨 Create `PhaseInteractionPanel_Skill.prefab`

| Property | Value |
|---|---|
| Components | `RectTransform` + `SkillPanel` MonoBehaviour |
| Parent | `SkillPanelAnchor` → `PanelDrawer._panel` reference |

> **Fix required first** — `SkillPanel.cs` has API mismatches with `ICombatSubsystem` (see Critical API Mismatches #1 above).

| Field on `SkillPanel` | Assign | Status |
|---|---|---|
| `_skillSlotContainer` | `SkillSlotContainer` Transform child | ⬜ |
| `_skillSlotPrefab` | `SkillSlot.prefab` (has `Button` + `NameText` TMP_Text + `CooldownText` TMP_Text + `Image`) | ⬜ |
| `_endTurnButton` | `Button_EndTurn` Button component | ⬜ |
| `_actorNameText` | `ActorNameText` TMP_Text | ⬜ |

> Injection (`ICombatSubsystem`, `IUnitSubsystem`, `ITargetingSubsystem`, `IGameStateSubsystem`,
> `INetworkManagerSubsystem`, `ICardLoadingManagerSubsystem`) via Zenject — no Inspector assignment.

### Wire `SkillPanelAnchor.prefab` (PanelDrawer)

| Field on `PanelDrawer` | Assign | Status |
|---|---|---|
| `_panel` | `PhaseInteractionPanel_Skill` RectTransform | ⬜ |
| `_toggle` | `Toggle_Sidebar` Toggle component on the anchor | ⬜ |

### Spec Verification

| Spec requirement | Status | Notes |
|---|---|---|
| Shows 6 slots | ✅ | `RenderSkills` iterates `unitData.Skills` list (up to 6) |
| Interactable only if CurrentActorCanMove/Act==true AND CD==0 | ✅ | `isReady = !isDisabled && !onCooldown`; `Button.interactable = isReady && IsLocalPlayerActor(actorId)` |
| Click → `ITargetingSubsystem.BeginTargeting` → confirm → `RequestSkill` | ✅ | `OnSkillClicked` → `_targeting.BeginTargeting` → callback `OnTargetConfirmed` → `_combat.RequestSkill` |

---

## F4.6 — Targeting Display

### TargetingOverlay wiring

| Field on `TargetingOverlay` | Assign | Status |
|---|---|---|
| `_tileHighlightPrefab` | `TileHighlight.prefab` (mesh with `Renderer`) | ⬜ |

> Add `TargetingOverlay` MonoBehaviour to a persistent GO in the Gameplay scene (e.g., `UI_Root` or `GameplayCanvas`).

> Injection (`ITargetingSubsystem`, `IBoardSubsystem`, `IUnitSubsystem`, `INetworkManagerSubsystem`) via Zenject — no Inspector assignment.

### Spec Verification

| Spec requirement | Status | Notes |
|---|---|---|
| Reads `display_pattern` | ⬜ **PARTIAL** | `TargetingRequest.DisplayPattern` is set from `skillId` if `skillData.display_pattern != null`, but the overlay does not use it to render pattern shapes; it renders hex-ring by range only. Display_pattern shape rendering is not implemented. |
| Yellow range, green valid, red invalid | ✅ | `_rangeColor`=yellow, `_validTargetColor`=green, `_invalidTargetColor`=red; applied per hover in `RefreshHighlightColors()` |
| Bitmask Enemy=1, Ally=2, EmptyTile=4 | ✅ | `ValidateSkillTarget` and `IsValidTarget` both check bitmask correctly |

---

## F4.7 — 3-Pass Damage Pipeline

### Spec Verification

| Spec requirement | Status | Notes |
|---|---|---|
| Pass 1 Aggregate | ✅ | `DamagePipelineSubsystem.Aggregate()` — friendly-fire returns 0 |
| Pass 2 Intercept: tile effects FIRST, then status effects | ✅ | `Intercept()` calls `InterceptByTileEffects` then `InterceptByStatusEffects` |
| Pass 3 Commit: `HP -= final`, clamped to 0 | ✅ | Returns `Mathf.Max(0, intercepted)`; caller calls `targetView.ServerApplyDamage(finalDamage)` |
| F4.11 friendly-fire block | ✅ | `Aggregate()` compares `sourceData.Owner == targetData.Owner`, returns 0 |

---

## F4.8 — Status Effects (SO Behaviors)

### Spec Verification

| Spec requirement | Status | Notes |
|---|---|---|
| `StatusEffectBehaviorSO` exists | ✅ | File present at `BehaviorRegistry/StatusEffectBehaviorSO.cs` |
| `barkskin_ward` reduces damage by 15 | ✅ | `InterceptByStatusEffects` subtracts 15 for `barkskin_ward` |
| `burning` deals 10 damage per turn | ✅ | `ApplyStartOfTurnEffects` applies 10 damage for `burning` |
| `decay` blocks healing | ✅ | Case handled in `InterceptByStatusEffects` (no reduction, comment notes blocks healing) |
| `rooted` prevents movement | ✅ | `HasStatus(data,"rooted")` pre-sets `CurrentActorHasMoved=true` |
| `melting` deals 20 damage per turn | ✅ | `ApplyStartOfTurnEffects` applies 20 for `melting` |

---

## F4.9 — Skill Cooldowns & One-Time

### Spec Verification

| Spec requirement | Status | Notes |
|---|---|---|
| `UnitController.OnTurnStart()` decrements all 6 slots | ⬜ **MISSING** | `UnitController.cs` has no `OnTurnStart()` method. Cooldown decrement is done directly in `CombatNetworkView.StartCurrentActorTurn()` via `unitView.ServerTickCooldowns()` (NetworkView method). The spec says `UnitController` owns this — it is implemented in the NetworkView instead, which is acceptable but diverges from spec. |
| `one_time:true` applies only to EquipSkills | ✅ | `skillData.one_time` sets `SkillOneTimeDisabled` flag; validated in `ServerSkill` |
| `CombatSubsystem.OnCombatPhaseEnd()` resets all one_time flags | ⬜ **MISSING** | `CombatSubsystem` has no `OnCombatPhaseEnd()` method. `ICombatSubsystem` interface does not declare it. One-time flags are never reset at end of combat phase. |

---

## F4.10 — Tile Effects (Lingering)

### Spec Verification

| Spec requirement | Status | Notes |
|---|---|---|
| `TileEffectSubsystem` exists | ✅ | Full stack: `TileEffectModel`, `TileEffectController`, `TileEffectSubsystem`, `TileEffectNetworkView` |
| Corrupted/Seeded/Melting effects supported | ✅ | `ApplyStartOfTurnEffects` handles `Corrupted` (10 dmg) and `Melting` (20 dmg); `InterceptByTileEffects` handles `Seeded` (−5 reduction) |
| One per tile | ✅ | `TryGet(coord)` returns single `TileEffectInstance` per coord |
| Duration tick | ✅ | `TickStatusDurations` runs per turn |

---

## F4.12 — Death & DeathAnchor

### Spec Verification

| Spec requirement | Status | Notes |
|---|---|---|
| `UnitController.OnHPZero()` destroys unit | ⬜ **MISSING** | `UnitController` has no `OnHPZero()`. Death detection is in `CombatNetworkView.ProcessDeath()` and `CheckAllUnitDeaths()`. Acceptable divergence from spec (authority stays in NetworkView), but `UnitController` does not participate. |
| DeathAnchor subtracted from owner HP | ✅ | `ProcessDeath()` reads `data.DeathAnchor`, calls `pczView.ServerApplyDamage(deathAnchor)` |
| Checks win condition after DeathAnchor | ✅ | `CheckWinCondition()` called when `pczView.HP <= 0` |

---

## F4.13 — Persistent Units

### Spec Verification

| Spec requirement | Status | Notes |
|---|---|---|
| `IsPersistent=true` survive board clear | ✅ | `CheckBoardClear()` skips units where `data.IsPersistent == true` |
| Persistent units excluded from non-player-count calculation | ✅ | `playersWithUnits` count skips persistent units |

---

## F4.14 — Verdant Evolution

### Spec Verification

| Spec requirement | Status | Notes |
|---|---|---|
| 4 Growth Stacks → swap unit identity | ✅ | `CheckEvolution()` checks `data.GrowthStacks >= 4` → looks up evolution chain |
| Evolution chain: Seedling → Sapling → YoungTreant → ThornColossus | ✅ | `GetEvolutionTarget()` switch expression is correct |
| `EvolutionBehaviorSO` exists | ✅ | File present at `BehaviorRegistry/EvolutionBehaviorSO.cs` |
| GrowthStacks reset to 0 after evolution | ✅ | `unitView.GrowthStacks = 0` after swap |

---

## F4.15 — Board Clear

### Spec Verification

| Spec requirement | Status | Notes |
|---|---|---|
| `CombatSubsystem.OnQueueExhausted()` when ≤1 player has units | ⬜ **PARTIAL** | Logic is in `CombatNetworkView.CheckBoardClear()` (called from `ServerEndCombatPhase`), not on `CombatSubsystem`. The subsystem has no `OnQueueExhausted` event. Behaviour is correct but exposed API diverges from spec. |
| Non-persistent → discard | ✅ | Non-persistent units despawned in `CheckBoardClear()` |
| Deploy area tile effects cleared | ✅ | `ClearDeployAreaEffects()` removes effects on deploy tiles |

---

## F4 — `CombatNetworkView` Prefab Wiring

### 🔨 Create `CombatNetworkView.prefab`

| Property | Value |
|---|---|
| Components | `NetworkObject` + `CombatNetworkView` + `GameObjectContext` + MonoInstaller (empty) |
| `NetworkObject.DestroyWhenStateAuthorityLeaves` | true |

| Field on `CombatNetworkView` | Assign | Status |
|---|---|---|
| `_turnDuration` | 30 (seconds per turn) | ⬜ |

> Injection (`ICombatSubsystem`, `IUnitSubsystem`, `IBoardSubsystem`, `IDamagePipelineSubsystem`,
> `ITileEffectSubsystem`, `ICardLoadingManagerSubsystem`, `IBehaviorRegistrySubsystem`, `IDebugLogger`)
> resolved via `SceneContext` fallback in `Spawned()` — no serialized refs on the prefab.

| Step | Status |
|---|---|
| Register `CombatNetworkView.prefab` in `NetworkViewRegistry` | ⬜ |
| Assign `CombatNetworkView.prefab` ref on `GameplayNetworkCoordinator._combatViewPrefab` | ⬜ |
| Add spawn call in `GameplayNetworkCoordinator` (or `GameStateNetworkView`) when transitioning to `CombatPhase` | ⬜ |
| Add despawn call / `ServerEndCombatPhase()` trigger at `CombatPhase` end | ⬜ |

---

## PanelVisibilityRouter — F4 Entries

Add to the `_phasePanels[]` array on `PanelVisibilityRouter` (in addition to F1–F3 entries):

| Entry | Phase enum value | Panel GameObject | Status |
|---|---|---|---|
| Skill anchor | `CombatPhase` | `SkillPanelAnchor` root | ⬜ |
| TurnOrder anchor | `CombatPhase` | `TurnOrderPanelAnchor` root | ⬜ |

> `SkillPanel` and `TurnOrderPanel` both self-manage `gameObject.SetActive` via `OnPhaseChanged`
> (set active only when `phase == CombatPhase`). The router entries are belt-and-suspenders
> but recommended for consistency with F3 panel behaviour.

---

## GameplayInstaller Bindings

### ✅ Already bound in `GameplayInstaller.cs`

| Class | Status |
|---|---|
| `CombatModel` | ✅ `BindInterfacesAndSelfTo<CombatModel>().AsSingle().NonLazy()` |
| `CombatController` | ✅ `BindInterfacesAndSelfTo<CombatController>().AsSingle().NonLazy()` |
| `CombatSubsystem` | ✅ `BindInterfacesAndSelfTo<CombatSubsystem>().AsSingle().NonLazy()` |
| `TileEffectModel/Controller/Subsystem` | ✅ bound |
| `DamagePipelineSubsystem` | ✅ bound |
| `BehaviorRegistryModel/Controller/Subsystem` | ✅ bound |
| `TargetingSubsystem` | ✅ bound |

**No missing DI bindings for F4.**

---

## Summary of Code Bugs to Fix Before Wiring

| # | Severity | Location | Issue |
|---|---|---|---|
| 1 | **Compile error** | `GameplayNetworkCoordinator.cs:33,251` | Stale compile — `CombatNetworkView` type not resolved. Fix: Assets → Reimport All. |
| 2 | **Compile error (latent)** | `SkillPanel.cs:53,54,74,220` | `CurrentTurnChanged` typed `UnityAction<NetworkId>` but handler expects `string`; `IsCombatActive`, `CurrentActorId`, `RequestEndTurn()` do not exist on `ICombatSubsystem`. |
| 3 | **Compile error (latent)** | `TurnOrderPanel.cs:54` | `QueueChanged` handler expects `IReadOnlyList<string>` but interface fires `IReadOnlyList<CombatQueueEntry>`. Also `IsCombatActive`, `CurrentActorId` missing on interface. |
| 4 | **Compile error (latent)** | `CombatNetworkView.cs` (PushState ~line 860) | `CombatStateData` has field `CurrentActor` (`NetworkId`); code writes `CurrentActorId` (`string`). Field name and type mismatch. `CombatStateData.HasMoved`/`HasActed` not populated in `PushState()`. |
| 5 | **Logic bug** | `ICombatSubsystem` / `CombatSubsystem` | `OnCombatPhaseEnd()` / reset-one-time-flags method missing. One-time equip skill flags are never cleared between combat phases. |
| 6 | **Logic bug** | `TargetingOverlay` | `display_pattern` tile shapes not rendered — range ring only. |
| 7 | **Missing feature** | `TurnOrderPanel` | Card art image not shown per entry — only name text. |
| 8 | **Missing feature** | `CombatNetworkView` / `GameplayNetworkCoordinator` | Spawn/despawn flow for `CombatNetworkView` not connected. `CombatNetworkView.prefab` does not exist yet. |
| 9 | **Missing feature** | F3 carry-over | Used Troop + EquipSpell cards not moved to discard after CombatPhase (noted in wiring-F3.md, still unresolved). |
