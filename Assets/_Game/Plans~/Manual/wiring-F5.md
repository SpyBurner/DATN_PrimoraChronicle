# Wiring — F5 Draw Phase

Legend: ⬜ todo · ✅ done

---

## Prefab: `CardSlot.prefab` (create if not already exists)

| Component | Note | Status |
|---|---|---|
| `Button` | — | ⬜ |
| `Image` | Background, tinted by selection state | ⬜ |
| `TMP_Text` child | Card name | ⬜ |

> Reuse the same `CardSlot.prefab` if HandPanel (F3) already created one.

---

## Prefab: `PhaseInteractionPanel_DrawCard.prefab` — `DrawPhasePanel`

| Task | Status |
|---|---|
| Add `DrawPhasePanel` MonoBehaviour to prefab root | ⬜ |

| Field | Assign | Status |
|---|---|---|
| `_cardSlotContainer` | `Content` Transform (inside ScrollView or HorizontalLayoutGroup) | ⬜ |
| `_cardSlotPrefab` | `CardSlot.prefab` | ⬜ |
| `_confirmButton` | `Button_Confirm` Button | ⬜ |
| `_keepCountText` | `Text_KeepCount` TMP_Text | ⬜ |

---

## Logic — Already Correct in Code

| Behaviour | Status |
|---|---|
| `PlayerCardZoneNetworkView.ServerDraw()` reshuffles Discard into Deck when Deck is empty | ✅ |
| `GameStateController` auto-advances DrawPhase on all-confirmed or timer expiry | ✅ |
