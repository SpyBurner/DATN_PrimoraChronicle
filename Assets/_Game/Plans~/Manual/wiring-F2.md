# Wiring — F2 Start Phase

Legend: ⬜ todo · ✅ done

---

## `GameplayDeckChooseNetworkView.prefab`

Location: `Assets/_Game/Features/Gameplay/Prefabs/NetworkedPrefabs/F1/GameplayDeckChooseNetworkView.prefab`

| Task | Status |
|---|---|
| Add `GameplayDeckChooseNetworkView` MonoBehaviour to prefab root | ⬜ |
| Register in Fusion `NetworkProjectConfig` → Prefabs tab | ⬜ |
| Assign to `GameplayNetworkCoordinator._deckChooseViewPrefab` on `GameplayCoordinator.prefab` | ⬜ |

> No serialized fields on this component — all injection resolved at runtime via SceneContext.

---

## `Overlay_Gameplay_Decks.prefab`

Location: `Assets/_Game/Features/Gameplay/UI/Component/Overlay_Gameplay_Decks.prefab`

| Task | Status |
|---|---|
| `_deckButtonPrefab` → `DeckButton` prefab | ✅ already assigned |
| `_deckSlot[0]` → `DeckSlot` child | ⬜ |
| `_deckSlot[1]` → `DeckSlot (1)` child | ⬜ |
| `_deckSlot[2]` → `DeckSlot (2)` child | ⬜ |
| `_deckSlot[3]` → `DeckSlot (3)` child | ⬜ |
| `_deckSlot[4]` → `DeckSlot (4)` child | ⬜ |
| `_deckSlot[5]` → `DeckSlot (5)` child | ⬜ |
| `_deckSlot[6]` → `DeckSlot (6)` child | ⬜ |
| `_deckSlot[7]` → `DeckSlot (7)` child | ⬜ |

---

## `PhaseInteractionPanel_DeckChoose.prefab`

| Field on `GameplayDeckChoosePanel` | Assign | Status |
|---|---|---|
| `_currentDeckButton` | `DeckButton` child | ✅ |
| `_deckSelectOverlay` | `GameplayDeckSelectOverlay` on `Overlay_Gameplay_Decks` | ✅ |
| `_timerText` | TMP_Text countdown child | ✅ |
| `_confirmButton` | Button child | ✅ |

---

## DI Bindings (`GameplayInstaller.cs`)

| Binding | Status |
|---|---|
| `GameplayDeckSubsystem` | ✅ |
| `GameplayDeckChooseModel` | ✅ |
| `GameplayDeckChooseController` | ✅ |
| `GameplayDeckChooseSubsystem` | ✅ |
