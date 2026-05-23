# Manual Unity Editor Wiring — F3 Main Phase

**Scope:** Everything needed to run F3.1–F3.6 in the Editor (Hand panel, Fusion staging UI,
Fusion authority, MainPhase spell play, Champion pinning, ready/confirm + auto-advance).

**Prerequisite:** F1 + F2 wiring complete (SceneContext, GameplayInstaller, all F1/F2 prefabs registered).

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
| CS0246 `FusionNetworkView` not found | `GameplayNetworkCoordinator.cs` | 36 | Unity incremental-compiler stale state after merge — `FusionNetworkView` class exists in `FusePhase/FusionNetworkView.cs` (same `GameplayFeatures` assembly) but Unity has not re-emitted it. Fix: **Assets → Reimport All** or delete the `Library/ScriptAssemblies` cache and let Unity rebuild. |
| CS0246 `FusionNetworkView` not found | `GameplayNetworkCoordinator.cs` | 248 | Same root cause as above. |

**Latent bugs (will surface as additional compile errors once stale state is cleared):**

| Bug | File | Detail |
|---|---|---|
| `HandPanel` subscribes `_cardZone.HandChanged` | `UI/HandPanel.cs:27,36` | `IPlayerCardZoneSubsystem` exposes only `OwnHandChanged`. Member `HandChanged` does not exist. Must be renamed to `OwnHandChanged`. |
| `HandPanel` calls `_cardZone.GetHand(_localPlayer)` | `UI/HandPanel.cs:30,98` | `IPlayerCardZoneSubsystem` exposes only `GetOwnHand()` (no parameter). Must be changed to `_cardZone.GetOwnHand()`. |
| `HandPanel.OnHandChanged` signature `(PlayerRef, IReadOnlyList<string>)` | `UI/HandPanel.cs:50` | `OwnHandChanged` passes only `IReadOnlyList<string>` (no `PlayerRef`). Handler signature must drop the `PlayerRef` parameter. |

> **Do NOT wire anything in this file until the compile errors above are fixed.**

---

## F3.1 — Hand Panel

### 🔨 Create `PhaseInteractionPanel_Hand.prefab`

| Property | Value |
|---|---|
| Components | `RectTransform` + `HandPanel` MonoBehaviour |
| Parent | `HandPanelAnchor` → `PanelDrawer._panel` reference |

> **Fix required first** — `HandPanel.cs` has API mismatches with `IPlayerCardZoneSubsystem` (see Compile Status above).

| Field on `HandPanel` | Assign | Status |
|---|---|---|
| `_cardSlotContainer` | `CardSlotContainer` (Transform child) | ⬜ |
| `_cardSlotPrefab` | `CardSlot.prefab` (has `TMP_Text` + `CardDragHandle`) | ⬜ |
| `_handCountText` | `HandCountText` (TMP_Text child) | ⬜ |

> Injection (`IPlayerCardZoneSubsystem`, `IGameStateSubsystem`, `INetworkManagerSubsystem`,
> `ICardLoadingManagerSubsystem`) via Zenject SceneContext — no manual Inspector assignment.

### Wire `HandPanelAnchor.prefab` (PanelDrawer)

| Field on `PanelDrawer` | Assign | Status |
|---|---|---|
| `_panel` | `PhaseInteractionPanel_Hand` RectTransform | ⬜ |
| `_toggle` | `Toggle_Sidebar` Toggle component on the anchor | ⬜ |

---

## F3.2 — Fusion Staging UI

### 🔨 Create `PhaseInteractionPanel_Fusion.prefab`

| Property | Value |
|---|---|
| Components | `RectTransform` + `FusionPanel` MonoBehaviour |
| Note | Direct phase panel — no PanelDrawer anchor. Shown/hidden by `PanelVisibilityRouter`. |

| Field on `FusionPanel` | Assign | Status |
|---|---|---|
| `_timerText` | `TimeValueText` TMP_Text (MainPhase countdown) | ⬜ |
| `_unitSlot` | `UnitSlot` Transform child (base card display area) | ⬜ |
| `_unitNameText` | `UnitNameText` (TMP_Text child of `_unitSlot`) | ⬜ |
| `_unitStatsText` | `UnitStatsText` (TMP_Text child of `_unitSlot`) | ⬜ |
| `_normalAttackSlot` | `NormalAttackSlot` GameObject (Skills[1] base_normal_attack) | ⬜ |
| `_movementSlot` | `MovementSlot` GameObject (Skills[0] base_move) | ⬜ |
| `_fuseSlotContainer` | `FuseSlotContainer` Transform child | ⬜ |
| `_fuseSlotPrefab` | `FuseSlot.prefab` (has `FuseSlotUI` + `FuseSlotDropTarget`) | ⬜ |
| `_confirmButton` | `Button_Confirm` Button component | ⬜ |
| `_confirmText` | `Button_Confirm/ConfirmText` TMP_Text | ⬜ |
| `_handPanel` | `HandPanel` MonoBehaviour reference (drag-source) | ⬜ |

> Injection (`IFusionSubsystem`, `IPlayerCardZoneSubsystem`, `IGameStateSubsystem`,
> `INetworkManagerSubsystem`, `ICardLoadingManagerSubsystem`) via Zenject — no Inspector assignment.

> `PhaseInteractionPanel_Fusion.prefab` is **not drawer-wrapped**. It is a full-screen or large phase panel shown/hidden directly by `PanelVisibilityRouter` — the same pattern as `PhaseInteractionPanel_DeckChoose` and `PhaseInteractionPanel_DrawCard`. No anchor, no `PanelDrawer`, no Toggle_Sidebar.

No `PanelDrawer` wiring needed for this panel.

---

## F3.3 — Fusion Authority NetworkView

### 🔨 Create `FusionNetworkView.prefab`

| Property | Value |
|---|---|
| Components | `NetworkObject` + `FusionNetworkView` + `GameObjectContext` + MonoInstaller (empty) |
| `NetworkObject.DestroyWhenStateAuthorityLeaves` | true |

| Field on `FusionNetworkView` | Assign | Status |
|---|---|---|
| `_unitPrefab` | `UnitNetworkView.prefab` NetworkPrefabRef | ⬜ |

> Injection (`IFusionSubsystem`, `IUnitSubsystem`, `IBoardSubsystem`, `IGameStateSubsystem`,
> `ICardLoadingManagerSubsystem`, `IDebugLogger`) resolved via `SceneContext` fallback in
> `Spawned()` — no serialized refs on the prefab.

| Step | Status |
|---|---|
| Register `FusionNetworkView.prefab` in `NetworkViewRegistry` | ⬜ |
| Assign `FusionNetworkView.prefab` to `GameplayNetworkCoordinator._fusionViewPrefab` | ⬜ |

> **Spawn trigger:** `GameplayNetworkCoordinator` should spawn one `FusionNetworkView` per
> player when `StartPhase → MainPhase` transition fires. Verify this spawn call exists in
> coordinator's `TransitionTo(MainPhase)` or `OnPhaseChanged` hook.

#### F3.3 Spec Verification

| Spec requirement | Status | Notes |
|---|---|---|
| ≤1 unit/turn guard | ✅ | `HasFusedThisTurn` NetworkBool checked in `Rpc_RequestConfirmFusion` |
| Validates exactly 1 base card | ✅ | Empty `baseCardId` rejected; `ValidateBaseCard` checks `type == "troop"` or `"champion"` |
| ≤4 slots total (innate occupies 1) | ✅ | `slotsUsed` counts equips + innate, rejects if `> MaxEquipSlots (4)` |
| `grants_skill` occupies 1 slot | ✅ | `HasInnateSkill()` checks `data.grants_skill` non-empty |
| Server clears Deploy Area before spawn | ⬜ **MISSING** | `SpawnUnit()` reads `GetDeployArea()` but does NOT call any clear/despawn on an existing unit at that coordinate first. If a unit already occupies the deploy tile, it will be stacked. |
| Sends Troop+EquipSpells to discard at end of CombatPhase | ⬜ **MISSING** | `GetUsedCardIds()` exists and `GameplayNetworkCoordinator.ResetFusionViewsForNewTurn()` resets Networked props, but no code calls `PlayerCardZoneNetworkView.ServerDiscardFromHand` or moves cards to the discard pile after CombatPhase. This must be wired in the combat-end flow. |

---

## F3.4 — MainPhase Spell Play

#### Spec Verification

| Spec requirement | Status | Notes |
|---|---|---|
| `RequestPlayMainPhaseSpell(cardId, target)` on `IPlayerCardZoneSubsystem` | ✅ | Method exists on interface and subsystem; routes through `PlayerCardZoneNetworkView.Rpc_RequestPlayMainPhaseSpell` |
| Card moves to discard immediately on server | ✅ | `Rpc_RequestPlayMainPhaseSpell` calls `ServerDiscardFromHand(i)` before executing the behavior |
| Routes to `BehaviorRegistrySubsystem.TryGetMainPhaseSpellBehavior` | ✅ | `ExecuteMainPhaseSpell` resolves behavior via `_behaviorRegistry.TryGetMainPhaseSpellBehavior` |
| Server rejects if `PlayerReady[i] == true` | ⬜ **MISSING** | `Rpc_RequestPlayMainPhaseSpell` does NOT check `PlayerReady` before executing. Once a player confirms fusion and their ready flag is set, the server should reject further spell RPCs for that player. Add a `PlayerReady.Get(Object.InputAuthority.PlayerId)` guard at the top of the RPC. |

---

## F3.5 — Champion Always-Available in Fusion

#### Spec Verification

| Spec requirement | Status | Notes |
|---|---|---|
| Champion card pinned to base slot pool (not consumed from hand) | ✅ | `FusionNetworkView.SpawnUnit()` skips `ServerDiscardFromHand` for champion cards (only troop base cards are discarded). Champion ID stays in the hand array permanently. `HandPanel` re-renders on `OwnHandChanged` after fusion and the champion reappears as a normal drag source. No pinning UI needed — F3.3 owns this behavior. |

---

## F3.6 — Main-Phase Ready/Confirm + Auto-Advance

#### Spec Verification

| Spec requirement | Status | Notes |
|---|---|---|
| `Button_Confirm` calls `ConfirmFusion()` then `RequestSetLocalReady(true)` | ⬜ **PARTIAL** | `FusionPanel.OnConfirmClicked` calls `_fusion.ConfirmFusion()` but does NOT call `IGameStateSubsystem.RequestSetLocalReady(true)` afterward. The ready handshake is never sent. |
| Server rejects `RequestPlayMainPhaseSpell` once `PlayerReady[i] = true` | ⬜ **MISSING** | (Same as F3.4 — no guard in the RPC.) |
| Timer-0 fallback: auto-confirm fusion for unready players | ⬜ **PARTIAL** | `GameStateNetworkView.HandlePhaseTimeout(MainPhase)` transitions to `CombatPhase` but does NOT auto-confirm fusion (Champion + 0 EquipSpells) for unready players before transitioning. The `AutoConfirmUnreadyPlayers()` method only exists for `StartPhase` (deck choose). A parallel method for MainPhase fusion must be added and called before `TransitionTo(CombatPhase)`. |
| Once `PlayerReady[i] = true`, server locks further fusion changes | ✅ | `HasFusedThisTurn` is set to true and `Rpc_RequestConfirmFusion` returns early if already set. |

---

## GameplayInstaller Bindings

### ✅ Already bound in `GameplayInstaller.cs`

| Class | Status |
|---|---|
| `FusionModel` | ✅ `BindInterfacesAndSelfTo<FusionModel>().AsSingle().NonLazy()` |
| `FusionController` | ✅ `BindInterfacesAndSelfTo<FusionController>().AsSingle().NonLazy()` |
| `FusionSubsystem` | ✅ `BindInterfacesAndSelfTo<FusionSubsystem>().AsSingle().NonLazy()` |
| `BehaviorRegistryModel` | ✅ bound |
| `BehaviorRegistryController` | ✅ bound |
| `BehaviorRegistrySubsystem` | ✅ bound |
| `PlayerCardZoneModel/Controller/Subsystem` | ✅ bound |

**No missing DI bindings for F3.**

---

## PanelVisibilityRouter — F3 Entries

Add to the `_phasePanels[]` array on `PanelVisibilityRouter` (in addition to F1 entries):

| Entry | Phase enum value | Panel GameObject | Status |
|---|---|---|---|
| Hand anchor | `MainPhase` | `HandPanelAnchor` root | ⬜ |
| Fusion panel | `MainPhase` | `PhaseInteractionPanel_Fusion` root | ⬜ |

> `SkillPanelAnchor` is NOT listed here — it is CombatPhase only (see wiring-F4.md).
> `PhaseInteractionPanel_Fusion` is controlled directly by the router (no drawer/anchor wrapper).
>
> **MainPhase panel layout: HandPanelAnchor (left drawer, card drag source) + PhaseInteractionPanel_Fusion (direct, drop target). No other panels.**

---

## F3 — `FuseSlotDropTarget` Wiring

`FuseSlotDropTarget.cs` handles drag-drop from `HandPanel` → FuseSlot. It must be on the
`FuseSlot.prefab`. `_fusionPanel` and `_slotIndex` are set at runtime via `Initialize()` —
no Inspector assignment needed for these.

| Step | Status |
|---|---|
| Attach `FuseSlotDropTarget` to `FuseSlot.prefab` root | ⬜ |

`BaseSlotDropTarget.cs` handles drag-drop into the base unit slot. Attach to `_unitSlot` GameObject.

| Field on `BaseSlotDropTarget` | Assign | Status |
|---|---|---|
| `_fusionPanel` | Parent `FusionPanel` MonoBehaviour | ⬜ |

---

## Summary of Code Bugs to Fix Before Wiring

| # | Severity | Location | Issue |
|---|---|---|---|
| 1 | **Compile error** | `GameplayNetworkCoordinator.cs:36,248` | Stale compile — `FusionNetworkView` type not resolved. Fix: Assets → Reimport All. |
| 2 | **Compile error (latent)** | `HandPanel.cs:27,36,30,98,50` | `HandChanged` and `GetHand(PlayerRef)` do not exist on `IPlayerCardZoneSubsystem`. Rename to `OwnHandChanged` / `GetOwnHand()` and fix handler signature. |
| 3 | **Logic bug** | `FusionNetworkView.SpawnUnit()` | Deploy Area not cleared before spawning fused unit. |
| 4 | **Logic bug** | `PlayerCardZoneNetworkView.Rpc_RequestPlayMainPhaseSpell` | No `PlayerReady` guard — spells accepted even after fusion confirmed. |
| 5 | **Logic bug** | `FusionPanel.OnConfirmClicked()` | `RequestSetLocalReady(true)` not called after `ConfirmFusion()`. |
| 6 | **Logic bug** | `GameStateNetworkView.HandlePhaseTimeout(MainPhase)` | No auto-confirm fusion for unready players before `TransitionTo(CombatPhase)`. |
| 7 | ~~Missing feature~~ **N/A** | `FusionPanel` / F3.3 | Champion is never discarded from hand (server skips `ServerDiscardFromHand` for champions). Always available as a drag source from `HandPanel`. No separate pinning UI required. |
| 8 | **Missing feature** | Combat-end flow | Used Troop + EquipSpell cards not moved to discard after CombatPhase. |
