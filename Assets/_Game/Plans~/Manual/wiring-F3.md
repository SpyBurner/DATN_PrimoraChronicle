# Wiring — F3 Main Phase

Legend: ⬜ todo · ✅ done

> All code bugs from the previous audit (HandPanel API, FusionNetworkView deploy-area clear,
> PlayerReady guard in Rpc_RequestPlayMainPhaseSpell, auto-deploy on timeout) are **fixed**.

---

## Prefab: `FuseSlot.prefab`

| Task | Status |
|---|---|
| Add `FuseSlotDropTarget` component | ⬜ |
| Add `FuseSlotUI` component | ⬜ |
| Wire `FuseSlotUI.NameText` → `NameText` TMP_Text child | ⬜ |
| Wire `FuseSlotUI.Icon` → `Icon` Image child | ⬜ |
| Wire `FuseSlotUI.ClearButton` → `ClearButton` Button child | ⬜ |

> `FuseSlotDropTarget._fusionPanel` and `_slotIndex` are wired at runtime via `Initialize()` — no Inspector assignment needed.

---

## Prefab: `PhaseInteractionPanel_Fusion.prefab` — `FusionPanel`

| Field | Assign | Status |
|---|---|---|
| `_timerText` | `TimeValueText` TMP_Text | ⬜ |
| `_unitSlot` | `UnitSlot` Transform child | ⬜ |
| `_unitNameText` | `UnitSlot/UnitNameText` TMP_Text | ⬜ |
| `_unitStatsText` | `UnitSlot/UnitStatsText` TMP_Text | ⬜ |
| `_normalAttackSlot` | `NormalAttackSlot` GameObject | ⬜ |
| `_movementSlot` | `MovementSlot` GameObject | ⬜ |
| `_fuseSlotContainer` | `FuseSlotContainer` Transform | ⬜ |
| `_fuseSlotPrefab` | `FuseSlot.prefab` | ⬜ |
| `_confirmButton` | `Button_Confirm` Button | ⬜ |
| `_confirmText` | `Button_Confirm/ConfirmText` TMP_Text | ⬜ |
| `_handPanel` | `HandPanel` MonoBehaviour reference | ⬜ |

Attach `BaseSlotDropTarget` to the `UnitSlot` child:

| Field | Assign | Status |
|---|---|---|
| `BaseSlotDropTarget._fusionPanel` | parent `FusionPanel` MonoBehaviour | ⬜ |

---

## Prefab: `FusionNetworkView.prefab` (create new)

| Component / Field | Value | Status |
|---|---|---|
| `NetworkObject` (`DestroyWhenStateAuthorityLeaves` = true) | — | ⬜ |
| `FusionNetworkView` | — | ⬜ |
| `GameObjectContext` + empty MonoInstaller | — | ⬜ |
| `_unitPrefab` | `UnitNetworkView.prefab` NetworkPrefabRef | ⬜ |
| Register in `NetworkViewRegistry` | — | ⬜ |
| Assign to `GameplayNetworkCoordinator._fusionViewPrefab` | — | ⬜ |

---

## Prefab: `UnitNetworkView.prefab` (create new)

| Component / Field | Value | Status |
|---|---|---|
| `NetworkObject` | — | ⬜ |
| `UnitNetworkView` | — | ⬜ |
| `_meshRoot` | Child `Transform` named `MeshRoot` with `MeshFilter` + `MeshRenderer` components (mesh and materials swapped at runtime) | ⬜ |
| `GameObjectContext` + empty `MonoInstaller` | — | ⬜ |
| Register in `NetworkViewRegistry` | — | ⬜ |
| Assign to `FusionNetworkView._unitPrefab` | — | ⬜ |

> No static mesh in the prefab. `UnitNetworkView.Render()` calls `GameplayNetworkCoordinator.Instance.GetPlayerPieceConfig(playerIndex)` once `Owner != PlayerRef.None` and instantiates `MeshPrefab` under `_meshRoot`, applying `Material` to its `Renderer`. The `_meshApplied` guard prevents re-instantiation on subsequent `Render()` calls.

---

## Prefab: `PhaseInteractionPanel_Hand.prefab` — `HandPanel` (create new)

| Component | Status |
|---|---|
| `RectTransform` + `HandPanel` MonoBehaviour | ⬜ |

| Field | Assign | Status |
|---|---|---|
| `_cardSlotContainer` | `CardSlotContainer` Transform child | ⬜ |
| `_cardSlotPrefab` | `CardSlot.prefab` (has TMP_Text + CardDragHandle) | ⬜ |
| `_handCountText` | `HandCountText` TMP_Text child | ⬜ |

Wire `HandPanelAnchor.prefab` → `PanelDrawer`:

| Field | Assign | Status |
|---|---|---|
| `_panel` | `PhaseInteractionPanel_Hand` RectTransform | ⬜ |
| `_toggle` | `Toggle_Sidebar` Toggle | ⬜ |

---

## DI Bindings (`GameplayInstaller.cs`)

| Binding | Status |
|---|---|
| `FusionModel` | ✅ |
| `FusionController` | ✅ |
| `FusionSubsystem` | ✅ |
| `PlayerCardZoneModel / Controller / Subsystem` | ✅ |
| `BehaviorRegistryModel / Controller / Subsystem` | ✅ |

---

## Logic — Already Correct in Code

| Behaviour | Status |
|---|---|
| `FusionPanel.OnConfirmClicked` calls `ConfirmFusion()` then `RequestSetLocalReady(true)` | ✅ |
| `FusionNetworkView.SpawnUnit()` clears deploy area before spawn | ✅ |
| `Rpc_RequestPlayMainPhaseSpell` rejects if `PlayerReady[i] == true` | ✅ |
| `HandlePhaseTimeout(MainPhase)` auto-deploys unready players before `CombatPhase` | ✅ |
| Champion card never discarded from hand — always available as drag source | ✅ |
