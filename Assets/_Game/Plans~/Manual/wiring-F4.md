# Wiring — F4 Combat Phase

Legend: ⬜ todo · ✅ done

> All code bugs from the previous audit (SkillPanel/TurnOrderPanel API mismatches,
> CombatNetworkView PushState field mismatch) are **fixed**.
> Minor spec deviation: one-time flag reset is in `CombatNetworkView` (not on `ICombatSubsystem` interface) — functionally correct.

---

## Prefab: `CombatNetworkView.prefab` (create new)

| Component / Field | Value | Status |
|---|---|---|
| `NetworkObject` (`DestroyWhenStateAuthorityLeaves` = true) | — | ⬜ |
| `CombatNetworkView` | — | ⬜ |
| `GameObjectContext` + empty MonoInstaller | — | ⬜ |
| `_turnDuration` | 30 | ⬜ |
| Register in `NetworkViewRegistry` | — | ⬜ |
| Assign to `GameplayNetworkCoordinator._combatViewPrefab` | — | ⬜ |
| Add spawn call in `GameplayNetworkCoordinator` on `CombatPhase` entry | — | ⬜ |
| Add `ServerEndCombatPhase()` trigger at `CombatPhase` end | — | ⬜ |

---

## Prefab: `PhaseInteractionPanel_Skill.prefab` — `SkillPanel` (create new)

| Component | Status |
|---|---|
| `RectTransform` + `SkillPanel` MonoBehaviour | ⬜ |

| Field | Assign | Status |
|---|---|---|
| `_skillSlotContainer` | `SkillSlotContainer` Transform child | ⬜ |
| `_skillSlotPrefab` | `SkillSlot.prefab` (Button + NameText TMP_Text + CooldownText TMP_Text + Image) | ⬜ |
| `_endTurnButton` | `Button_EndTurn` Button | ⬜ |
| `_actorNameText` | `ActorNameText` TMP_Text | ⬜ |

Wire `SkillPanelAnchor.prefab` → `PanelDrawer`:

| Field | Assign | Status |
|---|---|---|
| `_panel` | `PhaseInteractionPanel_Skill` RectTransform | ⬜ |
| `_toggle` | `Toggle_Sidebar` Toggle | ⬜ |

---

## Prefab: `PhaseInteractionPanel_TurnOrder.prefab` — `TurnOrderPanel` (create new)

| Component | Status |
|---|---|
| `RectTransform` + `TurnOrderPanel` MonoBehaviour | ⬜ |

| Field | Assign | Status |
|---|---|---|
| `_content` | `Content` Transform child (vertical layout group) | ⬜ |
| `_turnOrderItemPrefab` | `TurnOrderItem.prefab` (Image + TMP_Text) | ⬜ |

Wire `TurnOrderPanelAnchor.prefab` → `PanelDrawer`:

| Field | Assign | Status |
|---|---|---|
| `_panel` | `PhaseInteractionPanel_TurnOrder` RectTransform | ⬜ |
| `_toggle` | `Toggle_Sidebar` Toggle | ⬜ |

---

## Prefab: `TileHighlight.prefab` (create new)

Save to `Assets/_Game/Features/Gameplay/UI/Component/TileHighlight.prefab`.

| Step | Detail | Status |
|---|---|---|
| Root GameObject `TileHighlight` | Add `TileHighlight` MonoBehaviour (script exists) | ⬜ |
| Child GameObject `Mesh` | Add `MeshFilter` (hex mesh from `DATN_Environment.fbx`, same mesh used by `IM_Tile`) + `MeshRenderer` (URP/Unlit, Surface Type = Transparent) | ⬜ |
| Adjust child rotation | Tune in Editor to align with board tiles — reference `IM_Tile` which is spawned at `Euler(270, 330, 0)`. Root stays at identity; child absorbs the rotation offset | ⬜ |
| Wire `TileHighlight._renderer` | Drag child `MeshRenderer` into `_renderer` field on the root `TileHighlight` component | ⬜ |

## Scene: `TargetingOverlay`

| Task | Status |
|---|---|
| Add `TargetingOverlay` MonoBehaviour to a persistent GameObject in the Gameplay scene (e.g., `UI_Root`) | ⬜ |
| Wire `_tileHighlightPrefab` → `TileHighlight.prefab` (field type is `TileHighlight` component, not `GameObject`) | ⬜ |

---

## DI Bindings (`GameplayInstaller.cs`)

| Binding | Status |
|---|---|
| `CombatModel / Controller / Subsystem` | ✅ |
| `TileEffectModel / Controller / Subsystem` | ✅ |
| `DamagePipelineSubsystem` | ✅ |
| `BehaviorRegistryModel / Controller / Subsystem` | ✅ |
| `TargetingSubsystem` | ✅ |

---

## Logic — Already Correct in Code

| Behaviour | Status |
|---|---|
| `BuildQueue()` sorts Speed desc → HP asc → coin toss | ✅ |
| Turn cycle resets HasMoved/HasActed; rooted unit pre-sets HasMoved=true | ✅ |
| All 6 skill CDs ticked on turn start via `ServerTickCooldowns()` | ✅ |
| 3-pass damage pipeline (Aggregate → Intercept tile → Intercept status → Commit) | ✅ |
| Status effects: burning/melting/barkskin_ward/decay/rooted | ✅ |
| DeathAnchor subtracted from owner HP on unit death | ✅ |
| Persistent units survive board clear | ✅ |
| Verdant evolution at 4 Growth Stacks | ✅ |
| One-time flag reset implemented in `CombatNetworkView.ResetOneTimeFlagsOnPersistentUnits()` | ✅ |
