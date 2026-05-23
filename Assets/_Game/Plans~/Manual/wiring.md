# Wiring — F1 Foundation

Legend: ⬜ todo · ✅ done

---

## Scene Setup

| Task | Status |
|---|---|
| Add `SceneContext` GameObject; attach `GameplayInstaller` as MonoInstaller | ⬜ |
| Add `NetworkSceneManagerDefault` to the NetworkRunner host GameObject | ⬜ |
| Place `GameplayCoordinator.prefab` in scene | ⬜ |

---

## Prefab: `IM_Tile.prefab`

| Task | Status |
|---|---|
| Add `NetworkObject` component | ⬜ |
| Add `HexTile` MonoBehaviour | ⬜ |
| Register in `NetworkViewRegistry` | ⬜ |

---

## Prefab: `BoardManager.prefab` (create new)

| Component / Field | Value | Status |
|---|---|---|
| `NetworkObject` (`DestroyWhenStateAuthorityLeaves` = true) | — | ⬜ |
| `BoardNetworkView` | — | ⬜ |
| `GameObjectContext` + `BoardNetworkViewInstaller` | — | ⬜ |
| `_hexTilePrefab` | `IM_Tile.prefab` | ⬜ |
| `_horizontalSpacing` | 1.732 | ⬜ |
| `_verticalSpacing` | 1.5 | ⬜ |
| Register in `NetworkViewRegistry` | — | ⬜ |
| Assign to `GameplayNetworkCoordinator._boardManagerPrefab` | — | ⬜ |

---

## Prefab: `GameStateManager.prefab` (create new)

| Component / Field | Value | Status |
|---|---|---|
| `NetworkObject` | — | ⬜ |
| `GameStateNetworkView` | — | ⬜ |
| `GameObjectContext` + `GameStateNetworkViewInstaller` | — | ⬜ |
| `_startPhaseDuration` | 30 | ⬜ |
| `_mainPhaseDuration` | 60 | ⬜ |
| `_drawPhaseDuration` | 30 | ⬜ |
| `_matchTimeLimit` | 3600 | ⬜ |
| Register in `NetworkViewRegistry` | — | ⬜ |
| Assign to `GameplayNetworkCoordinator._gameStateManagerPrefab` | — | ⬜ |

---

## Prefab: `PlayerRosterPublicState.prefab` (create new)

| Component / Field | Value | Status |
|---|---|---|
| `NetworkObject` (`DestroyWhenStateAuthorityLeaves` = true) | — | ⬜ |
| `PlayerRosterPublicNetworkView` | — | ⬜ |
| Register in `NetworkViewRegistry` | — | ⬜ |
| Assign to `GameplayNetworkCoordinator._playerRosterPublicViewPrefab` | — | ⬜ |

---

## Prefab: `PlayerCardZonePrivateState.prefab` (create new)

| Component / Field | Value | Status |
|---|---|---|
| `NetworkObject` (`DestroyWhenStateAuthorityLeaves` = true) | — | ⬜ |
| `PlayerCardZoneNetworkView` | — | ⬜ |
| Register in `NetworkViewRegistry` | — | ⬜ |
| Assign to `GameplayNetworkCoordinator._playerCardZoneViewPrefab` | — | ⬜ |

---

## Prefab: `MatchRewardsPrivateState.prefab` (create new)

| Component / Field | Value | Status |
|---|---|---|
| `NetworkObject` (`DestroyWhenStateAuthorityLeaves` = true) | — | ⬜ |
| `MatchRewardsPrivateNetworkView` | — | ⬜ |
| Register in `NetworkViewRegistry` | — | ⬜ |
| Assign to `GameplayNetworkCoordinator._matchRewardsPrivateViewPrefab` | — | ⬜ |

---

## Prefab: `GameplayCoordinator.prefab` (create new)

| Component | Status |
|---|---|
| `NetworkObject` | ⬜ |
| `GameplayNetworkCoordinator` | ⬜ |

| Field | Assign | Status |
|---|---|---|
| `_gameStateManagerPrefab` | `GameStateManager.prefab` | ⬜ |
| `_boardManagerPrefab` | `BoardManager.prefab` | ⬜ |
| `_deckChooseViewPrefab` | `GameplayDeckChooseNetworkView.prefab` | ⬜ |
| `_playerCardZoneViewPrefab` | `PlayerCardZonePrivateState.prefab` | ⬜ |
| `_playerRosterPublicViewPrefab` | `PlayerRosterPublicState.prefab` | ⬜ |
| `_matchRewardsPrivateViewPrefab` | `MatchRewardsPrivateState.prefab` | ⬜ |
| `_fusionViewPrefab` | `FusionNetworkView.prefab` *(wire in F3)* | ⬜ |
| `_combatViewPrefab` | `CombatNetworkView.prefab` *(wire in F4)* | ⬜ |
| `_matchResultCoordinatorPrefab` | `MatchResultState.prefab` *(wire in F6)* | ⬜ |

> Leave `_player1PiecePrefab` / `_player2PiecePrefab` empty — legacy fields, not needed.

---

## Prefab: `Layout_Fullscreen_Gameplay.prefab` — `GameplayHUDController`

| Task | Status |
|---|---|
| Add `GameplayHUDController` component to prefab root | ⬜ |

| Field | Assign | Status |
|---|---|---|
| `_phaseNameText` | `PhaseNameValueText` TMP_Text | ⬜ |
| `_matchTimeText` | `MatchTimeValueText` TMP_Text | ⬜ |
| `_localProfile` | `Profile_Player` → `GameplayPlayerProfileUI` component | ⬜ |
| `_opponentProfile` | `Profile_Enemy1` → `GameplayPlayerProfileUI` component | ⬜ |
| `_enemy2ProfileRoot` | `Profile_Enemy2` root GameObject | ⬜ |

---

## Prefab: `Profile_Gameplay.prefab` — `GameplayPlayerProfileUI`

Add `GameplayPlayerProfileUI` to **each** profile prefab instance (`Profile_Player` and `Profile_Enemy1`).

| Field | Assign | Status |
|---|---|---|
| `_nameText` | `NameValueText` TMP_Text | ⬜ |
| `_hpText` | `HPValueText` TMP_Text | ⬜ |
| `_readyToggle` | `ReadyToggle` Toggle (forced non-interactable at runtime) | ⬜ |
| `_avatarImage` | avatar `RawImage` child (optional) | ⬜ |

---

## Scene: `PanelVisibilityRouter` (empty GameObject)

Add `PanelVisibilityRouter` component. Wire `_phasePanels[]`:

| Phase | Panel GameObject | Status |
|---|---|---|
| `StartPhase` | `PhaseInteractionPanel_DeckChoose` root | ⬜ |
| `MainPhase` | `HandPanelAnchor` root | ⬜ |
| `MainPhase` | `PhaseInteractionPanel_Fusion` root | ⬜ |
| `CombatPhase` | `SkillPanelAnchor` root | ⬜ |
| `CombatPhase` | `TurnOrderPanelAnchor` root | ⬜ |
| `DrawPhase` | `HandPanelAnchor` root | ⬜ |
| `DrawPhase` | `PhaseInteractionPanel_DrawCard` root | ⬜ |
| `GameOver` | `PhaseInteractionPanel_MatchResult` root | ⬜ |
