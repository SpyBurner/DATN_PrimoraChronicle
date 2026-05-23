# Manual Unity Editor Wiring — F1 Foundation

**Scope:** Everything needed to run F1.1–F1.5 in the Editor.
**Rule:** Do NOT touch anything under `LEGACY/`. All legacy NetworkObjects (`NetworkPlayerState`, `NetworkGameplayManager`, `NetworkSpawner`) are deprecated and must not be in the Gameplay scene.

---

## Legend

| Symbol | Meaning |
|---|---|
| ⬜ | Not yet wired |
| ✅ | Wired and verified |
| 🔨 | Requires a new prefab to be created first |

---

## F1.1 — Scene Bootstrap

### Gameplay Scene setup

| What | Action | Status |
|---|---|---|
| `SceneContext` GameObject | Must exist in scene; references `GameplayInstaller` | ⬜ |
| `GameplayInstaller` (MonoInstaller) | Attach to `SceneContext` GameObject or a child; no serialized fields needed — all bindings are code-only | ⬜ |
| `NetworkSceneManagerDefault` | Add to the scene's Runner or a dedicated `NetworkRunner` host GameObject | ⬜ |
| `GameplayCoordinator` (NetworkObject) | Place `GameplayCoordinator.prefab` in scene — **this is the root spawner; it must be a NetworkObject** | ⬜ |

> **Note on `_playerStatePrefab` in `GameplayNetworkCoordinator`:** Leave this field **empty/unassigned**. It is a legacy holdover from `NetworkPlayerState` (LEGACY). The field will be removed in a cleanup pass. `PlayerRosterPublicNetworkView` is the F1 replacement.

---

## F1.2 — Hex Board Generation

### Step 1 — Create `IM_Tile.prefab` (if not already a NetworkObject prefab)

The legacy `HexTile.prefab` visuals can be reused. The prefab must:
- Have a `NetworkObject` component
- Have a `HexTile` MonoBehaviour attached (already in `LEGACY/_NetworkMono/HexTile.cs` — reference-only, still used)
- Be registered in the scene's `NetworkViewRegistry` (or Fusion's prefab table)

| Step | Status |
|---|---|
| Confirm `IM_Tile.prefab` exists as a NetworkObject with `HexTile` component | ⬜ |
| Register `IM_Tile.prefab` in `NetworkViewRegistry` | ⬜ |

### Step 2 — Create `BoardManager.prefab`

| Property | Value |
|---|---|
| Components | `NetworkObject` + `BoardNetworkView` + `GameObjectContext` + `BoardNetworkViewInstaller` |
| `NetworkObject.DestroyWhenStateAuthorityLeaves` | true |

| Field on `BoardNetworkView` | Assign | Status |
|---|---|---|
| `_hexTilePrefab` | `IM_Tile.prefab` | ⬜ |
| `_autoMeasureSpacing` | ✅ true (auto-measures from tile renderer bounds) | — |
| `_horizontalSpacing` | 1.732 (fallback if auto-measure fails) | ⬜ |
| `_verticalSpacing` | 1.5 (fallback) | ⬜ |

| Step | Status |
|---|---|
| Register `BoardManager.prefab` in `NetworkViewRegistry` | ⬜ |
| Assign `BoardManager.prefab` to `GameplayNetworkCoordinator._boardManagerPrefab` | ⬜ |

---

## F1.3 — Phase Machine + Match Timer

### Step 1 — Create `GameStateManager.prefab`

| Property | Value |
|---|---|
| Components | `NetworkObject` + `GameStateNetworkView` + `GameObjectContext` + `GameStateNetworkViewInstaller` |

| Field on `GameStateNetworkView` | Value | Status |
|---|---|---|
| `_startPhaseDuration` | 30 | ⬜ |
| `_mainPhaseDuration` | 60 | ⬜ |
| `_drawPhaseDuration` | 30 | ⬜ |
| `_matchTimeLimit` | 3600 | ⬜ |

> Injection (`IGameStateSubsystem`, `IDebugLogger`) is resolved at runtime via SceneContext fallback in `Spawned()`. No serialized subsystem refs on the prefab.

| Step | Status |
|---|---|
| Register `GameStateManager.prefab` in `NetworkViewRegistry` | ⬜ |
| Assign `GameStateManager.prefab` to `GameplayNetworkCoordinator._gameStateManagerPrefab` | ⬜ |

---

## F1.2 + F1.3 Shared — `GameplayNetworkCoordinator` (on its own NetworkObject prefab in the scene)

Create `GameplayCoordinator.prefab` (NetworkObject + `GameplayNetworkCoordinator`).

| Field | Assign | Status |
|---|---|---|
| `_gameStateManagerPrefab` | `GameStateManager.prefab` | ⬜ |
| `_boardManagerPrefab` | `BoardManager.prefab` | ⬜ |
| `_playerStatePrefab` | **Leave empty** — legacy field, do not assign | — |
| `_deckChooseViewPrefab` | `GameplayDeckChooseNetworkView.prefab` (from DeckChoose stack) | ⬜ |
| `_playerCardZoneViewPrefab` | `PlayerCardZonePrivateState.prefab` (see F1.2 PlayerCardZone) | ⬜ |
| `_playerRosterPublicViewPrefab` | 🔨 `PlayerRosterPublicState.prefab` (create below) | ⬜ |
| `_matchRewardsPrivateViewPrefab` | 🔨 `MatchRewardsPrivateState.prefab` (create below) | ⬜ |
| `_player1PiecePrefab` | Player 1 piece NetworkObject prefab (F3 Main Phase) | ⬜ |
| `_player2PiecePrefab` | Player 2 piece NetworkObject prefab (F3 Main Phase) | ⬜ |

> **Note on Player Pieces:** Spawning the physical player pieces on the board belongs to the **F3 (Main Phase)** group, specifically during the Fusion "drop-pod" mechanic. You can leave `_player1PiecePrefab` and `_player2PiecePrefab` empty for now.

Place `GameplayCoordinator.prefab` in the Gameplay scene (it auto-spawns everything when the Host's `Spawned()` runs).

---

## F1.2 — PlayerRoster (new for F1)

### 🔨 Create `PlayerRosterPublicState.prefab`

| Property | Value |
|---|---|
| Components | `NetworkObject` + `PlayerRosterPublicNetworkView` |
| `NetworkObject.DestroyWhenStateAuthorityLeaves` | true |
| Injection | Resolved at runtime from SceneContext — no serialized refs needed |

| Step | Status |
|---|---|
| Create prefab | ⬜ |
| Register in `NetworkViewRegistry` | ⬜ |
| Assign to `GameplayNetworkCoordinator._playerRosterPublicViewPrefab` | ⬜ |

---

## F1.2 — PlayerCardZone

### 🔨 Create `PlayerCardZonePrivateState.prefab`

| Property | Value |
|---|---|
| Components | `NetworkObject` + `PlayerCardZoneNetworkView` |
| `NetworkObject.DestroyWhenStateAuthorityLeaves` | true |
| Injection | Resolved at runtime from SceneContext — no serialized refs needed |

| Step | Status |
|---|---|
| Create prefab | ⬜ |
| Register in `NetworkViewRegistry` | ⬜ |
| Assign to `GameplayNetworkCoordinator._playerCardZoneViewPrefab` | ⬜ |

---

## F1.2 — MatchRewards (new for F1)

### 🔨 Create `MatchRewardsPrivateState.prefab`

| Property | Value |
|---|---|
| Components | `NetworkObject` + `MatchRewardsPrivateNetworkView` |
| `NetworkObject.DestroyWhenStateAuthorityLeaves` | true |
| Injection | Resolved at runtime from SceneContext — no serialized refs needed |

| Step | Status |
|---|---|
| Create prefab | ⬜ |
| Register in `NetworkViewRegistry` | ⬜ |
| Assign to `GameplayNetworkCoordinator._matchRewardsPrivateViewPrefab` | ⬜ |

---

## F1.4 — HUD Shell

Wire `GameplayHUDController` on the root of `Layout_Fullscreen_Gameplay.prefab`.

| Field | Child object to assign | Status |
|---|---|---|
| `_phaseNameText` | `PhaseNameValueText` (TMP_Text) | ⬜ |
| `_matchTimeText` | `MatchTimeValueText` (TMP_Text) | ⬜ |
| `_localProfile` | `Profile_Player` child's `GameplayPlayerProfileUI` component | ⬜ |
| `_opponentProfile` | `Profile_Enemy1` child's `GameplayPlayerProfileUI` component | ⬜ |
| `_enemy2ProfileRoot` | `Profile_Enemy2` root GameObject (will be hidden at runtime) | ⬜ |

> Injection (`IGameStateSubsystem`, `INetworkManagerSubsystem`) via Zenject SceneContext — no manual assignment.

---

## F1.5 — Profile Bridge to HUD

Wire `GameplayPlayerProfileUI` on the root of **each** `Profile_Gameplay.prefab` instance (`Profile_Player` and `Profile_Enemy1`).

| Field | Child object to assign | Status |
|---|---|---|
| `_nameText` | `NameValueText` (TMP_Text) | ⬜ |
| `_hpText` | `HPValueText` (TMP_Text) | ⬜ |
| `_readyToggle` | `ReadyToggle` (Toggle — `interactable` will be forced false at runtime) | ⬜ |
| `_avatarImage` | `Panel` or avatar `RawImage` child — **optional**, can be null | ⬜ |

> Injection (`IPlayerRosterSubsystem`, `IGameStateSubsystem`, `IProfileSubsystem`) via Zenject SceneContext — no manual assignment.
>
> `Bind(playerRef, isLocal)` is called by `GameplayHUDController` at runtime once the Runner reports both players — no Inspector binding needed for which player this profile represents.

---

## F1 — PanelVisibilityRouter

Wire `PanelVisibilityRouter` on an empty GameObject in the Gameplay scene.

The router drives `SetActive` on anchor/panel root GameObjects. Multiple entries with the same Phase are all shown simultaneously. Each row is one array element.

| `_phasePanels[]` entry | Phase | GameObject to assign | Status |
|---|---|---|---|
| — | `StartPhase` | `PhaseInteractionPanel_DeckChoose` root | ⬜ |
| — | `MainPhase` | `HandPanelAnchor` root | ⬜ |
| — | `MainPhase` | `PhaseInteractionPanel_Fusion` root | ⬜ |
| — | `CombatPhase` | `SkillPanelAnchor` root | ⬜ |
| — | `CombatPhase` | `TurnOrderPanelAnchor` root | ⬜ |
| — | `DrawPhase` | `HandPanelAnchor` root | ⬜ |
| — | `DrawPhase` | `PhaseInteractionPanel_DrawCard` root | ⬜ |
| — | `GameOver` | `PhaseInteractionPanel_MatchResult` root | ⬜ |

> **Phase layout summary (source of truth: `panel_details.md`):**
> - StartPhase: `PhaseInteractionPanel_DeckChoose` only
> - MainPhase: `HandPanelAnchor` (drawer, card drag source) + `PhaseInteractionPanel_Fusion` (direct, drop target)
> - CombatPhase: `SkillPanelAnchor` (current actor's skills) + `TurnOrderPanelAnchor`
> - DrawPhase: `HandPanelAnchor` (view/keep hand) + `PhaseInteractionPanel_DrawCard`
> - GameOver: `PhaseInteractionPanel_MatchResult` only
>
> Drawer-wrapped panels (Hand, Skill, TurnOrder) use their anchor root as the router target. Direct panels (DeckChoose, Fusion, DrawCard, MatchResult) are router targets themselves — no anchor wrapper.
---

## F2+ Wiring

*(Added when F2–F6 are implemented.)*
