# Manual Unity Editor Wiring — F5 Draw Phase

**Scope:** Everything needed to run F5.1–F5.3 in the Editor (Draw 2 + hand-keep UI, reshuffle on empty deck, draw-phase ready/confirm + auto-advance).
**Prerequisite:** F1 wiring complete (`PlayerCardZonePrivateState.prefab` exists and is registered).

---

## Legend

| Symbol | Meaning |
|---|---|
| ⬜ | Not yet wired |
| ✅ | Wired and verified |
| 🔨 | Requires a new prefab to be created first |

---

## F5.1 — Draw Phase Panel (`PhaseInteractionPanel_DrawCard.prefab`)

Open `Assets/_Game/Features/Gameplay/UI/Component/PhaseInteractionPanel_DrawCard.prefab`.

### Step 1 — Add `DrawPhasePanel` component

Add `DrawPhasePanel` MonoBehaviour to the root (or a dedicated child GameObject) of the prefab.

All four injected dependencies (`IPlayerCardZoneSubsystem`, `IGameStateSubsystem`, `INetworkManagerSubsystem`, `ICardLoadingManagerSubsystem`) are resolved at runtime via Zenject SceneContext — no serialized refs needed for them.

### Step 2 — Wire serialized fields on `DrawPhasePanel`

| Field | Type | Child object to assign | Status |
|---|---|---|---|
| `_cardSlotContainer` | `Transform` | A `ScrollView > Viewport > Content` or a plain `HorizontalLayoutGroup` child that will hold spawned card slots | ⬜ |
| `_cardSlotPrefab` | `GameObject` | A prefab with a `Button` + `Image` + `TMP_Text` (one slot per card). Must be a Project asset, not a scene object | ⬜ |
| `_confirmButton` | `Button` | `Button_Confirm` child Button | ⬜ |
| `_keepCountText` | `TMP_Text` | A `TMP_Text` label that displays `kept/6` — e.g. `Text_KeepCount` | ⬜ |

> **Selection Visuals** (`_selectedColor`, `_discardedColor`) have sensible defaults (green / grey) and do not need Inspector assignment unless you want to override the colors.

> `DrawPhasePanel` auto-selects all cards on display and allows toggling individual cards off (discard). Confirm is blocked if `selected > 6`. Deselected cards are sent to discard via `IPlayerCardZoneSubsystem.RequestKeepCards(player, keep)`.

---

## F5.2 — Reshuffle on Empty Deck (`PlayerCardZoneNetworkView`)

No prefab changes required. The reshuffle logic lives inside `PlayerCardZoneNetworkView.ServerDraw()` (server-side code path). Verify the implementation path:

| Check | Status |
|---|---|
| `PlayerCardZoneNetworkView.ServerDraw()` shuffles `Discard` into `Deck` before drawing when `Deck` is empty | ⬜ |
| `PlayerCardZonePrivateState.prefab` registered in `NetworkViewRegistry` (already covered by F1) | ⬜ |
| `GameplayNetworkCoordinator._playerCardZoneViewPrefab` assigned `PlayerCardZonePrivateState.prefab` (already covered by F1) | ⬜ |

---

## F5.3 — Draw-Phase Ready / Confirm + Auto-Advance

### Panel confirm flow

`DrawPhasePanel.OnConfirmClicked()` calls `IPlayerCardZoneSubsystem.RequestKeepCards(player, keep)` — this fires the RPC that sets `DrawPhaseConfirmed = true` on the server. There is no second call to `IGameStateSubsystem.RequestSetLocalReady` needed from the panel; the server-side `GameStateController` watches `DrawPhaseConfirmed` for all players and advances the phase.

Verify the following server-side wiring is in place:

| Check | Status |
|---|---|
| `GameStateController` (or `GameStateNetworkView`) subscribes to `IPlayerCardZoneSubsystem.DrawPhaseConfirmedChanged` and calls `SetLocalReady` / advances phase when all players confirmed | ⬜ |
| Timer-0 fallback: `GameStateController` calls auto-confirm for remaining unready players when `DrawPhase` timer expires | ⬜ |

---

## F5 — PanelVisibilityRouter Entry

Open the `PanelVisibilityRouter` GameObject in the Gameplay scene (added in F1 wiring).

| `_phasePanels[]` entry | Phase enum value | Panel GameObject | Status |
|---|---|---|---|
| Entry 3 | `DrawPhase` | `PhaseInteractionPanel_DrawCard` root GameObject | ⬜ |

> Entry 3 was already listed as a placeholder in the F1 wiring doc. Assign the actual `PhaseInteractionPanel_DrawCard` GameObject here.

---

## F5 — `GameplayNetworkCoordinator` Reference Check

No new fields are added to `GameplayNetworkCoordinator` for F5. Confirm the existing field from F1 is assigned:

| Field | Assign | Status |
|---|---|---|
| `_playerCardZoneViewPrefab` | `PlayerCardZonePrivateState.prefab` | ⬜ |

---

## F5 — Card Slot Prefab (new asset)

🔨 Create a `CardSlot.prefab` (UI prefab) if it does not already exist:

| Property | Value |
|---|---|
| Components | `Button` + `Image` (background, tinted by selection state) + child `TMP_Text` (card name) |
| Pivot / Anchor | Top-left, sized to fit inside `_cardSlotContainer` layout group |

| Step | Status |
|---|---|
| Create `CardSlot.prefab` as a Project asset | ⬜ |
| Assign to `DrawPhasePanel._cardSlotPrefab` on `PhaseInteractionPanel_DrawCard.prefab` | ⬜ |

---

## Summary Checklist

| Step | Status |
|---|---|
| Add `DrawPhasePanel` component to `PhaseInteractionPanel_DrawCard.prefab` | ⬜ |
| Wire `_cardSlotContainer` → content transform | ⬜ |
| Wire `_cardSlotPrefab` → `CardSlot.prefab` | ⬜ |
| Wire `_confirmButton` → `Button_Confirm` | ⬜ |
| Wire `_keepCountText` → keep-count TMP label | ⬜ |
| Verify `PlayerCardZoneNetworkView.ServerDraw()` reshuffles on empty deck | ⬜ |
| Verify `GameStateController` auto-advances phase on all-confirmed or timer-0 | ⬜ |
| Assign `PhaseInteractionPanel_DrawCard` in `PanelVisibilityRouter._phasePanels[3]` | ⬜ |
| Confirm `GameplayNetworkCoordinator._playerCardZoneViewPrefab` is assigned | ⬜ |
