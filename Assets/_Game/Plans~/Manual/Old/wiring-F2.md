# Manual Unity Editor Wiring — F2 Start Phase

**Scope:** Everything needed to run F2.1–F2.4 (Deck-Choose, NetworkView spawn, card-zone setup, auto-confirm) in the Editor.
**Prerequisite:** F1 wiring (`wiring.md`) must be complete first.

---

## Legend

| Symbol | Meaning |
|---|---|
| ⬜ | Not yet wired |
| ✅ | Wired and verified in prefab asset |
| 🔨 | Requires a new prefab to be created first |

---

## Compile Status

**Clean — 0 errors.** 10 warnings (all `CS0618 FindFirstObjectByType` obsolete + 3 unused-event `CS0067`). No action required for F2.

---

## Audit Findings (Issues to be aware of)

| # | File | Issue |
|---|---|---|
| 1 | `GameplayDeckChooseNetworkView.prefab` | Prefab exists but has only `NetworkObject` — `GameplayDeckChooseNetworkView` MonoBehaviour is **missing**. Must be added. |
| 2 | `Overlay_Gameplay_Decks.prefab` | `GameplayDeckSelectOverlay._deckSlot[0..7]` are all `{fileID: 0}` — none of the 8 slot GameObjects are assigned. Must be wired. |
| 3 | `GameStateController` | Does **not** call `IGameplayDeckChooseSubsystem.AutoConfirmLastDeck()` on timer expiry. Auto-confirm is routed through `GameStateNetworkView.AutoConfirmUnreadyPlayers()` → `GameplayDeckChooseNetworkView.ServerAutoConfirm()` directly (bypasses the subsystem). Diverges from F2.4 spec, but functional. No code change needed for wiring. |
| 4 | `PlayerCardZoneController` | Has no `SetupDeckForMatch` method. Setup is done server-side on `PlayerCardZoneNetworkView.ServerSetupDeckForMatch()` (called by `GameplayDeckChooseNetworkView.SetupPlayerDeck()`). Diverges from F2.3 spec naming but equivalent logic is present. No code change needed for wiring. |
| 5 | `GameplayNetworkCoordinator` | Spawns `GameplayDeckChooseNetworkView` per player in `SpawnPlayerState()` — **not** gated on `StartPhase`. All views are spawned on match start. This is acceptable for the current scope. |

---

## F2.1 — Deck Selection Panel

### Step 1 — `GameplayDeckChooseNetworkView.prefab` (fix missing component)

Prefab location: `Assets/_Game/Features/Gameplay/Prefabs/NetworkedPrefabs/F1/GameplayDeckChooseNetworkView.prefab`

| Step | Action | Status |
|---|---|---|
| Open prefab in Inspector | Select the root GameObject | ⬜ |
| Add Component | `GameplayDeckChooseNetworkView` (from `GameplayFeatures` assembly) | ⬜ |
| Add to Fusion NetworkPrefab table | Open `Fusion NetworkProjectConfig` asset → `Prefabs` tab → add `GameplayDeckChooseNetworkView.prefab` | ⬜ |

> No serialized fields on `GameplayDeckChooseNetworkView` — injection is resolved at runtime via `[Inject(Optional = true)]` and SceneContext fallback in `Spawned()`.

---

### Step 2 — `PhaseInteractionPanel_DeckChoose.prefab` (already wired — verify)

Prefab location: `Assets/_Game/Features/Gameplay/UI/Component/PhaseInteractionPanel_DeckChoose.prefab`

`GameplayDeckChoosePanel` component is already added and fields are serialized in the prefab asset. Verify the following are correct in the Inspector:

| Field on `GameplayDeckChoosePanel` | Expected assignment | Status |
|---|---|---|
| `_currentDeckButton` | `DeckButton` child inside the panel | ✅ (fileID wired in prefab) |
| `_deckSelectOverlay` | `GameplayDeckSelectOverlay` component on `Overlay_Gameplay_Decks` prefab instance | ✅ (fileID wired in prefab) |
| `_timerText` | `TMP_Text` child for countdown display | ✅ (fileID wired in prefab) |
| `_confirmButton` | `Button` child for confirm action | ✅ (fileID wired in prefab) |

> Injection (`IGameplayDeckSubsystem`, `IGameplayDeckChooseSubsystem`, `IGameStateSubsystem`) comes from Zenject SceneContext — no manual assignment needed.

---

### Step 3 — `Overlay_Gameplay_Decks.prefab` (wire 8 deck slots)

Prefab location: `Assets/_Game/Features/Gameplay/UI/Component/Overlay_Gameplay_Decks.prefab`

`GameplayDeckSelectOverlay` component exists on the prefab root. `_deckButtonPrefab` is already wired to the `DeckButton` prefab. **All 8 `_deckSlot` entries are null — must be wired.**

The prefab contains 8 child GameObjects named `DeckSlot`, `DeckSlot (1)` … `DeckSlot (7)`. Wire each into the corresponding array slot:

| `_deckSlot` index | Assign | Status |
|---|---|---|
| `[0]` | `DeckSlot` child GameObject | ⬜ |
| `[1]` | `DeckSlot (1)` child GameObject | ⬜ |
| `[2]` | `DeckSlot (2)` child GameObject | ⬜ |
| `[3]` | `DeckSlot (3)` child GameObject | ⬜ |
| `[4]` | `DeckSlot (4)` child GameObject | ⬜ |
| `[5]` | `DeckSlot (5)` child GameObject | ⬜ |
| `[6]` | `DeckSlot (6)` child GameObject | ⬜ |
| `[7]` | `DeckSlot (7)` child GameObject | ⬜ |

| Field | Status |
|---|---|
| `_deckButtonPrefab` | ✅ Already assigned to `DeckButton` prefab |

---

## F2.2 — NetworkView Spawn Trigger

`GameplayNetworkCoordinator.SpawnPlayerState()` already spawns one `GameplayDeckChooseNetworkView` per player via `_deckChooseViewPrefab`. Wiring is on the `GameplayCoordinator.prefab` (created in F1 wiring).

| Field on `GameplayNetworkCoordinator` | Assign | Status |
|---|---|---|
| `_deckChooseViewPrefab` | `GameplayDeckChooseNetworkView.prefab` | ⬜ (must assign after Step 1 above adds the MonoBehaviour) |

> This was also listed in `wiring.md` F1 coordinator table. If already assigned there, re-verify after adding the MonoBehaviour in Step 1 — the prefab GUID is unchanged so the reference should remain valid.

---

## F2.3 — Granted-Cards Shuffle + Opening Hand

All logic is implemented server-side in `PlayerCardZoneNetworkView.ServerSetupDeckForMatch()`. It:
- Reads `grants_cards` from `ICardLoadingManagerSubsystem`
- Shuffles the full deck via `ShuffleDeck()`
- Deals 6 cards via `ServerDraw(6)` (constant `OpeningHandSize = 6`)
- Sets HP from champion card data (`champData.hp`) or falls back to `DefaultHP = 100`

No prefab wiring needed for F2.3. Verify the following runtime dependencies are present in the scene:

| Dependency | Where | Status |
|---|---|---|
| `ICardLoadingManagerSubsystem` bound | `CoreInstaller` (ProjectContext) | ✅ (global binding) |
| `PlayerCardZonePrivateState.prefab` registered in Fusion | `NetworkProjectConfig` → Prefabs | ⬜ (verify — part of F1 wiring) |

---

## F2.4 — Auto-Confirm on Timer Expiry

Handled in `GameStateNetworkView.HandlePhaseTimeout()` → `AutoConfirmUnreadyPlayers()` → `GameplayDeckChooseNetworkView.ServerAutoConfirm()`.

No prefab wiring needed. Runtime prerequisite: `GameplayNetworkCoordinator.Instance` must be non-null when `StartPhase` timer expires (it is a singleton on the coordinator prefab).

| Check | Status |
|---|---|
| `GameplayCoordinator.prefab` is in the Gameplay scene | ⬜ (verify — part of F1 wiring) |
| `_deckChooseViewPrefab` assigned on coordinator | ⬜ (see F2.2 above) |

---

## F2 — GameplayInstaller Bindings (already present — verify)

`GameplayInstaller.cs` already contains all required F2 bindings:

| Binding | Status |
|---|---|
| `GameplayDeckSubsystem` | ✅ |
| `GameplayDeckChooseModel` | ✅ |
| `GameplayDeckChooseController` | ✅ |
| `GameplayDeckChooseSubsystem` | ✅ |

No code changes needed.

---

## F2 — Interface Files Location (already correct — verify)

All 5 interface/data files are in `Core/Scripts/Interfaces/Features/Gameplay/StartPhase/`:

| File | Status |
|---|---|
| `IGameplayDeckChooseSubsystem.cs` | ✅ |
| `IGameplayDeckChooseController.cs` | ✅ |
| `IGameplayDeckChooseModel.cs` | ✅ |
| `IGameplayDeckChooseNetworkBridge.cs` | ✅ |
| `GameplayDeckChooseStateData.cs` | ✅ |
| `IGameplayDeckSubsystem.cs` | ✅ (bonus — also in StartPhase/) |

---

## Summary — Wiring Checklist

| # | Task | Status |
|---|---|---|
| 1 | Add `GameplayDeckChooseNetworkView` MonoBehaviour to `GameplayDeckChooseNetworkView.prefab` | ⬜ |
| 2 | Register `GameplayDeckChooseNetworkView.prefab` in Fusion NetworkProjectConfig | ⬜ |
| 3 | Wire `Overlay_Gameplay_Decks.prefab` `_deckSlot[0..7]` to their child GameObjects | ⬜ |
| 4 | Assign `GameplayDeckChooseNetworkView.prefab` to `GameplayNetworkCoordinator._deckChooseViewPrefab` | ⬜ |
| 5 | Verify `PanelVisibilityRouter` entry 0 points to `PhaseInteractionPanel_DeckChoose` (listed in wiring.md F1) | ⬜ |
