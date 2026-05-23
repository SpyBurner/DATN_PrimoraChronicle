# Manual Unity Editor Wiring — F6 Match End

**Scope:** Everything needed to run F6.1–F6.3 (Win Condition, Match Result Panel, Backend Report + Shutdown) in the Editor.
**Prerequisites:** F1 wiring complete (GameplayCoordinator prefab in scene, NetworkViewRegistry exists, PanelVisibilityRouter wired).

---

## Legend

| Symbol | Meaning |
|---|---|
| ⬜ | Not yet wired |
| ✅ | Wired and verified |
| 🔨 | Requires a new prefab to be created first |

---

## Spec Deviation — GoldEarned / XPEarned on GameMatchResult

**Per plan F6.3**, `GoldEarned` and `XPEarned` belong in `MatchRewardsPrivateData` (per-player private rewards, delivered via `MatchRewardsPrivateNetworkView`). They are **not** match-wide shared data.

**As merged**, `GameMatchResult` struct (`Core/Scripts/Interfaces/Features/Gameplay/MatchResult/GameMatchResult.cs`) carries `GoldEarned` and `XPEarned` directly, and `MatchResultNetworkView` syncs them as `[Networked]` props broadcast to all clients. `MatchResultPanel.DisplayResult()` reads `result.GoldEarned` / `result.XPEarned` from `GameMatchResult` rather than subscribing to `IMatchRewardsSubsystem.OwnRewardsReceived`.

**Do NOT change this.** The code compiles clean. Document it and move on. The practical effect is that every client sees the winner's reward values — acceptable for a 2-player game.

---

## F6.1 — Win Condition

Win condition logic lives entirely in `GameStateNetworkView.CommitMatchResult()` (server-authoritative). No Inspector wiring is required for this feature — it runs through existing `GameStateNetworkView` and `GameplayNetworkCoordinator` fields already wired under F1/F3.

Verify the following already-wired field is assigned (carried over from F1 wiring):

| Field on `GameplayNetworkCoordinator` | Assign | Status |
|---|---|---|
| `_matchResultCoordinatorPrefab` | `MatchResultState.prefab` (see F6.3 below) | ⬜ |

---

## F6.2 — Match Result Panel

### Step 1 — Create `PhaseInteractionPanel_MatchResult.prefab`

Build the prefab in `Assets/_Game/Features/Gameplay/UI/Component/` (alongside the other phase panels). Minimum hierarchy:

```
PhaseInteractionPanel_MatchResult (root)
├── Player0Slot
│   ├── Player0Crown          (Image)
│   ├── Player0PFP            (Image)
│   └── Player0NameText       (TMP_Text)
├── Player1Slot
│   ├── Player1Crown          (Image)
│   ├── Player1PFP            (Image)
│   └── Player1NameText       (TMP_Text)
├── Player2Slot               (set inactive by default — 2-player game only)
│   ├── Player2Crown          (Image)
│   ├── Player2PFP            (Image)
│   └── Player2NameText       (TMP_Text)
├── RewardsGroup
│   ├── GoldValueText         (TMP_Text)
│   ├── XPValueText           (TMP_Text)
│   └── TimeValueText         (TMP_Text)
└── Button_Confirm            (Button)
```

Add `MatchResultPanel` MonoBehaviour to the root. Wire all `[SerializeField]` fields:

| Field on `MatchResultPanel` | Assign | Status |
|---|---|---|
| `_player0Crown` | `Player0Slot/Player0Crown` (Image) | ⬜ |
| `_player0PFP` | `Player0Slot/Player0PFP` (Image) | ⬜ |
| `_player0Name` | `Player0Slot/Player0NameText` (TMP_Text) | ⬜ |
| `_player1Crown` | `Player1Slot/Player1Crown` (Image) | ⬜ |
| `_player1PFP` | `Player1Slot/Player1PFP` (Image) | ⬜ |
| `_player1Name` | `Player1Slot/Player1NameText` (TMP_Text) | ⬜ |
| `_player2Crown` | `Player2Slot/Player2Crown` (Image) | ⬜ |
| `_player2PFP` | `Player2Slot/Player2PFP` (Image) | ⬜ |
| `_player2Name` | `Player2Slot/Player2NameText` (TMP_Text) | ⬜ |
| `_goldValueText` | `RewardsGroup/GoldValueText` (TMP_Text) | ⬜ |
| `_xpValueText` | `RewardsGroup/XPValueText` (TMP_Text) | ⬜ |
| `_timeValueText` | `RewardsGroup/TimeValueText` (TMP_Text) | ⬜ |
| `_confirmButton` | `Button_Confirm` (Button) | ⬜ |
| `_player0Slot` | `Player0Slot` (GameObject) | ⬜ |
| `_player1Slot` | `Player1Slot` (GameObject) | ⬜ |
| `_player2Slot` | `Player2Slot` (GameObject) | ⬜ |

> `_player2Slot` is force-hidden in `OnEnable()`. Set it inactive in the prefab as well.

> Zenject injections (`IMatchResultSubsystem`, `IGameStateSubsystem`, `IProfileSubsystem`, `INetworkManagerSubsystem`) are resolved at runtime via SceneContext — no manual assignment.

---

## F6.3 — MatchResultNetworkView Prefab (MatchResultState.prefab)

### 🔨 Create `MatchResultState.prefab`

| Property | Value |
|---|---|
| Components | `NetworkObject` + `MatchResultNetworkView` + `GameObjectContext` |
| `NetworkObject.DestroyWhenStateAuthorityLeaves` | true |
| Injection | Resolved at runtime from SceneContext fallback in `Spawned()` — no serialized refs needed |

> `GameObjectContext` is required because `MatchResultNetworkView` uses `[Inject(Optional = true)]`. Without a `GameObjectContext` the Zenject injection path is not entered and the `SceneContext` fallback inside `Spawned()` handles it. Including `GameObjectContext` is still the safe default for NetworkObject prefabs to keep the stack consistent.

| Step | Status |
|---|---|
| Create prefab with `NetworkObject` + `MatchResultNetworkView` + `GameObjectContext` | ⬜ |
| Register `MatchResultState.prefab` in `NetworkViewRegistry` | ⬜ |
| Assign `MatchResultState.prefab` to `GameplayNetworkCoordinator._matchResultCoordinatorPrefab` | ⬜ |

---

## F6.3 — MatchRewardsPrivateState.prefab

This prefab is already documented in `wiring.md` (F1.2 — MatchRewards section). It is reproduced here for completeness because F6 is the feature that actually uses it.

### 🔨 Create `MatchRewardsPrivateState.prefab`

| Property | Value |
|---|---|
| Components | `NetworkObject` + `MatchRewardsPrivateNetworkView` |
| `NetworkObject.DestroyWhenStateAuthorityLeaves` | true |
| Injection | Resolved at runtime from SceneContext — no serialized refs needed |

> Host spawns **one per connected player** and calls `ServerInitialize(playerRef)` to set AoI. `GameplayNetworkCoordinator` handles this in its player-join loop.

| Step | Status |
|---|---|
| Create prefab with `NetworkObject` + `MatchRewardsPrivateNetworkView` | ⬜ |
| Register `MatchRewardsPrivateState.prefab` in `NetworkViewRegistry` | ⬜ |
| Assign `MatchRewardsPrivateState.prefab` to `GameplayNetworkCoordinator._matchRewardsPrivateViewPrefab` | ⬜ |

---

## F6 — GameplayNetworkCoordinator Field Summary

These two fields on `GameplayCoordinator.prefab` must be assigned for F6 to function:

| Field | Assign | Status |
|---|---|---|
| `_matchResultCoordinatorPrefab` | `MatchResultState.prefab` | ⬜ |
| `_matchRewardsPrivateViewPrefab` | `MatchRewardsPrivateState.prefab` | ⬜ |

> All other coordinator fields are carried from F1–F5 wiring docs.

---

## F6 — PanelVisibilityRouter

Add (or confirm) the `GameOver` entry in `PanelVisibilityRouter._phasePanels[]`. This entry is already listed in `wiring.md` (F1 section) as Entry 4. Verify it is assigned:

| `_phasePanels[]` entry | Phase enum value | Panel GameObject | Status |
|---|---|---|---|
| Entry 4 | `GameOver` | `PhaseInteractionPanel_MatchResult` | ⬜ |

> `MatchResultPanel.OnPhaseChanged()` also self-manages its own `SetActive` on phase change as a safety net, but the router is the canonical show/hide path.

---

## F6 — DI Bindings (GameplayInstaller)

Already merged and present in `GameplayInstaller.cs`. No action required.

| Binding | Status |
|---|---|
| `MatchRewardsModel` / `MatchRewardsController` / `MatchRewardsSubsystem` | ✅ |
| `MatchResultModel` / `MatchResultController` / `MatchResultSubsystem` | ✅ |
