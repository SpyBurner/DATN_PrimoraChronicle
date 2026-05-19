# F5 — Draw Phase: Prefab Wiring & Script Attachment Instructions

## Overview

The Draw Phase panel displays all cards in the player's hand after 2 new cards are drawn. If the hand exceeds 6 cards, the player must deselect cards to discard down to the hand max (6). Cards are toggled by clicking — selected cards are kept (green), deselected cards are discarded (greyed out).

---

## 1. DrawPhasePanel.cs → PhaseInteractionPanel_DrawCard.prefab

### Attach Script
- Open `Assets/_Game/Features/Gameplay/UI/Component/PhaseInteractionPanel_DrawCard.prefab`
- Add component **DrawPhasePanel** to the **root GameObject**

### Serialize Field Wiring

| Field | Target in Prefab Hierarchy | Notes |
|---|---|---|
| `_cardSlotContainer` | `Panel (1)` | The parent transform containing the 6 CardSlot placeholders. Cards will be spawned as children here at runtime. |
| `_cardSlotPrefab` | One of the existing `CardSlot` children (extract as a prefab) **OR** reuse the same card slot prefab used by `HandPanel` | Must have: `Button` component on root, `TMP_Text` in children for card name, `Image` on root for selection coloring. |
| `_confirmButton` | `Panel/Button_Confirm` | The confirm button. |
| `_keepCountText` | *(Optional)* Create a `TMP_Text` child under `Panel` if you want a "3/6" counter display. Can be left null. |
| `_selectedColor` | Default: green `(0.2, 0.8, 0.2, 1.0)` | Override in Inspector if needed |
| `_discardedColor` | Default: grey `(0.4, 0.4, 0.4, 0.6)` | Override in Inspector if needed |

### CardSlot Prefab Requirements

The `_cardSlotPrefab` needs:
- **Root**: `Button` component + `Image` component (used for selection color feedback)
- **Child**: `TMP_Text` for displaying card name (found via `GetComponentInChildren<TMP_Text>`)
- Optionally a `CardDisplay` component if you want illustration/description support

If reusing the same prefab as `HandPanel._cardSlotPrefab`, that already satisfies these requirements.

---

## 2. PanelVisibilityRouter — Register DrawPhasePanel

The `PanelVisibilityRouter` component (already in scene) controls which phase panel is visible.

- In the scene's `PanelVisibilityRouter` Inspector, add an entry to the `_phasePanels` array:
  - **Phase**: `DrawPhase`
  - **Panel**: Drag the `PhaseInteractionPanel_DrawCard` GameObject

This ensures the panel is only visible during the Draw Phase.

---

## 3. Delete Placeholder CardSlots (Optional)

The prefab has 6 pre-placed `CardSlot` children (`CardSlot`, `CardSlot (1)` through `CardSlot (5)`). These are **design-time placeholders**.

**Option A — Keep them**: Delete them at runtime. The script clears `_cardSlotContainer` children and spawns fresh slots from the prefab.

**Option B — Remove them**: Delete all 6 placeholder CardSlots from the prefab. The script dynamically spawns exactly as many slots as there are cards in hand.

**Recommended**: Option B (remove placeholders) for cleaner runtime behavior.

---

## 4. Zenject Injection

`DrawPhasePanel` uses `[Inject]` attributes. It will be auto-injected by the scene's `SceneContext` as long as:
- The GameObject is a child of (or in the same scene as) the `SceneContext` that references `GameplayInstaller`
- No additional installer binding is needed for MonoBehaviours — Zenject's `SceneContext` auto-injects all `MonoBehaviour`s in the scene hierarchy

---

## 5. User Flow Summary

```
DrawPhase begins
    → PanelVisibilityRouter activates PhaseInteractionPanel_DrawCard
    → Server draws 2 cards → IPlayerCardZoneSubsystem.HandChanged fires
    → DrawPhasePanel.RefreshDisplay() shows all cards (e.g., 8 if hand was full)
    → All cards start SELECTED (green)
    → Player clicks cards to DESELECT (grey) until ≤ 6 remain selected
    → Player clicks Confirm
    → DrawPhasePanel calls RequestKeepCards(localPlayer, selectedCardIds)
    → Server processes → HandChanged fires with final 6 cards
    → Phase advances → PanelVisibilityRouter hides panel
```

---

## 6. File Locations

| File | Path |
|---|---|
| DrawPhasePanel.cs | `Assets/_Game/Features/Gameplay/Scripts/UI/DrawPhasePanel.cs` |
| Prefab to wire | `Assets/_Game/Features/Gameplay/UI/Component/PhaseInteractionPanel_DrawCard.prefab` |
| PanelVisibilityRouter | Already exists in Gameplay scene |
