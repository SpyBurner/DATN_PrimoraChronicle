# Wiring — F6 Match End

Legend: ⬜ todo · ✅ done

---

## Prefab: `PhaseInteractionPanel_MatchResult.prefab` (create new)

Minimum hierarchy:
```
PhaseInteractionPanel_MatchResult  (root + MatchResultPanel)
├── Player0Slot  (GameObject)
│   ├── Player0Crown   (Image)
│   ├── Player0PFP     (Image)
│   └── Player0NameText (TMP_Text)
├── Player1Slot  (GameObject)
│   ├── Player1Crown   (Image)
│   ├── Player1PFP     (Image)
│   └── Player1NameText (TMP_Text)
├── Player2Slot  (GameObject — set inactive by default)
│   ├── Player2Crown   (Image)
│   ├── Player2PFP     (Image)
│   └── Player2NameText (TMP_Text)
├── RewardsGroup
│   ├── GoldValueText  (TMP_Text)
│   ├── XPValueText    (TMP_Text)
│   └── TimeValueText  (TMP_Text)
└── Button_Confirm  (Button)
```

| Task | Status |
|---|---|
| Add `MatchResultPanel` MonoBehaviour to root | ⬜ |

| Field | Assign | Status |
|---|---|---|
| `_player0Slot` | `Player0Slot` GameObject | ⬜ |
| `_player0Crown` | `Player0Slot/Player0Crown` Image | ⬜ |
| `_player0PFP` | `Player0Slot/Player0PFP` Image | ⬜ |
| `_player0Name` | `Player0Slot/Player0NameText` TMP_Text | ⬜ |
| `_player1Slot` | `Player1Slot` GameObject | ⬜ |
| `_player1Crown` | `Player1Slot/Player1Crown` Image | ⬜ |
| `_player1PFP` | `Player1Slot/Player1PFP` Image | ⬜ |
| `_player1Name` | `Player1Slot/Player1NameText` TMP_Text | ⬜ |
| `_player2Slot` | `Player2Slot` GameObject | ⬜ |
| `_player2Crown` | `Player2Slot/Player2Crown` Image | ⬜ |
| `_player2PFP` | `Player2Slot/Player2PFP` Image | ⬜ |
| `_player2Name` | `Player2Slot/Player2NameText` TMP_Text | ⬜ |
| `_goldValueText` | `RewardsGroup/GoldValueText` TMP_Text | ⬜ |
| `_xpValueText` | `RewardsGroup/XPValueText` TMP_Text | ⬜ |
| `_timeValueText` | `RewardsGroup/TimeValueText` TMP_Text | ⬜ |
| `_confirmButton` | `Button_Confirm` Button | ⬜ |

---

## Prefab: `MatchResultState.prefab` (create new)

| Component / Field | Value | Status |
|---|---|---|
| `NetworkObject` (`DestroyWhenStateAuthorityLeaves` = true) | — | ⬜ |
| `MatchResultNetworkView` | — | ⬜ |
| `GameObjectContext` | — | ⬜ |
| Register in `NetworkViewRegistry` | — | ⬜ |
| Assign to `GameplayNetworkCoordinator._matchResultCoordinatorPrefab` | — | ⬜ |

---

## Prefab: `MatchRewardsPrivateState.prefab` (create new — also listed in F1)

| Component / Field | Value | Status |
|---|---|---|
| `NetworkObject` (`DestroyWhenStateAuthorityLeaves` = true) | — | ⬜ |
| `MatchRewardsPrivateNetworkView` | — | ⬜ |
| Register in `NetworkViewRegistry` | — | ⬜ |
| Assign to `GameplayNetworkCoordinator._matchRewardsPrivateViewPrefab` | — | ⬜ |

---

## DI Bindings (`GameplayInstaller.cs`)

| Binding | Status |
|---|---|
| `MatchRewardsModel / Controller / Subsystem` | ✅ |
| `MatchResultModel / Controller / Subsystem` | ✅ |

---

## Note on GoldEarned / XPEarned

`GameMatchResult` carries `GoldEarned` and `XPEarned` directly (synced to all clients via `MatchResultNetworkView`). `MatchResultPanel` reads from `GameMatchResult`, not from `IMatchRewardsSubsystem`. This differs from the original plan spec but is intentional — acceptable for a 2-player game.
