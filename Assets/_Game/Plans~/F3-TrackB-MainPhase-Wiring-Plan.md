# F3 Track B — Main Phase UI: Manual Prefab Wiring Plan

## Overview

This document describes how to manually wire the F3 (Main Phase) Track B scripts onto their corresponding prefabs in Unity Editor.

**Scripts created:**
- `Features/Gameplay/Scripts/UI/HandPanel.cs`
- `Features/Gameplay/Scripts/UI/FusionPanel.cs`
- `Features/Gameplay/Scripts/UI/CardDragHandle.cs`
- `Features/Gameplay/Scripts/UI/FuseSlotDropTarget.cs`
- `Features/Gameplay/Scripts/UI/BaseSlotDropTarget.cs`

**Prefab paths (all under `Assets/_Game/Features/Gameplay/UI/Component/`):**
- `PhaseInteractionPanel_Hand.prefab`
- `PhaseInteractionPanel_Fusion.prefab`
- `HandPanelAnchor.prefab`

---

## 1. PhaseInteractionPanel_Hand.prefab → HandPanel

### Step 1: Add Component
1. Open `PhaseInteractionPanel_Hand.prefab`
2. Select the **root** GameObject
3. Add Component → `HandPanel`

### Step 2: Wire Serialized Fields

| Field | Assign to |
|---|---|
| `_cardSlotContainer` | The `Panel (content area)` Transform — this is where card slot items are spawned at runtime |
| `_cardSlotPrefab` | A card slot prefab (see §4 below for creating it) |
| `_handCountText` | (Optional) A TMP_Text showing "X/6" hand count — create one if desired, or leave null |

### Step 3: Zenject injection
HandPanel uses `[Inject]` so it requires the Gameplay scene's SceneContext to be active. No installer binding needed — MonoBehaviours on scene prefabs are auto-injected by Zenject's SceneContext.

---

## 2. HandPanelAnchor.prefab → PanelDrawer wrapper

The `HandPanelAnchor.prefab` wraps `PhaseInteractionPanel_Hand.prefab` via a `PanelDrawer`.

1. Open `HandPanelAnchor.prefab`
2. Ensure it has the `PanelDrawer` component already (should be wired via editor tool `Tools/Primora/Add PanelDrawers to Anchors`)
3. `PanelDrawer._panel` → the RectTransform of the embedded `PhaseInteractionPanel_Hand` child
4. `PanelDrawer._toggle` → `Toggle_Sidebar` component on the anchor
5. Ensure an `OpenPosition` child exists with the desired open anchored position

---

## 3. PhaseInteractionPanel_Fusion.prefab → FusionPanel

### Step 1: Add Component
1. Open `PhaseInteractionPanel_Fusion.prefab`
2. Select the **root** GameObject
3. Add Component → `FusionPanel`

### Step 2: Wire Serialized Fields

Based on the prefab hierarchy from `Gameplay_UI_Panels_details.md`:

```
Panel/Panel/TimeValueText         — timer (not directly on FusionPanel, skip)
Panel/Button_Cancel               — not wired to FusionPanel (use for ClearStaging if needed)
Panel/Button_Confirm              — _confirmButton
Panel (1)/Panel/UnitSlot          — _unitSlot (Transform)
Panel (1)/Panel (1)/Panel (2)/NormalAttackSlot — _normalAttackSlot (GameObject)
Panel (1)/Panel (1)/Panel (2)/MovementSlot     — _movementSlot (GameObject)
Panel (1)/Panel (1)/Panel (3)/FuseSlot1        — _fuseSlots[0].Root
Panel (1)/Panel (1)/Panel (3)/FuseSlot2        — _fuseSlots[1].Root
Panel (1)/Panel (1)/Panel (3)/FuseSlot3        — _fuseSlots[2].Root
Panel (1)/Panel (1)/Panel (3)/FuseSlot4        — _fuseSlots[3].Root
```

| Field | Assign to |
|---|---|
| `_unitSlot` | `Panel (1)/Panel/UnitSlot` (Transform) |
| `_unitNameText` | A TMP_Text child inside `UnitSlot` (create one if not present, or use existing CardDisplay text) |
| `_unitStatsText` | A TMP_Text child inside `UnitSlot` for stats like "HP:40 SPD:2.0 ATK:10" |
| `_normalAttackSlot` | `Panel (1)/Panel (1)/Panel (2)/NormalAttackSlot` (GameObject) |
| `_movementSlot` | `Panel (1)/Panel (1)/Panel (2)/MovementSlot` (GameObject) |
| `_fuseSlots[0].Root` | `FuseSlot1` GameObject |
| `_fuseSlots[0].NameText` | TMP_Text child inside `FuseSlot1` |
| `_fuseSlots[0].ClearButton` | A small "X" Button inside `FuseSlot1` (create if not present) |
| `_fuseSlots[1].*` | Same pattern for `FuseSlot2` |
| `_fuseSlots[2].*` | Same pattern for `FuseSlot3` |
| `_fuseSlots[3].*` | Same pattern for `FuseSlot4` |
| `_confirmButton` | `Panel/Button_Confirm` (Button component) |
| `_confirmText` | TMP_Text child of `Button_Confirm` |
| `_handPanel` | Reference to the `HandPanel` component (from the scene instance, or leave null if not needed) |

### Step 3: Add Drop Targets

For each `FuseSlot1..4`:
1. Select the slot GameObject (e.g. `FuseSlot1`)
2. Add Component → `FuseSlotDropTarget`
3. Set `_slotIndex` = 0 (for FuseSlot1), 1 (for FuseSlot2), etc.
4. Set `_fusionPanel` = the root FusionPanel component

For `UnitSlot` (the base/troop drop target):
1. Select `UnitSlot` GameObject
2. Add Component → `BaseSlotDropTarget`
3. Set `_fusionPanel` = the root FusionPanel component

---

## 4. CardSlot Prefab (shared by HandPanel)

Create a new prefab `CardSlot_Hand.prefab` for use by `HandPanel._cardSlotPrefab`:

### Structure:
```
CardSlot_Hand (RectTransform, Image, Button, CanvasGroup, CardDragHandle)
├── CardNameText (TMP_Text)
└── CardIcon (Image, optional)
```

### Setup:
1. Create a new GameObject in the scene
2. Add: `RectTransform`, `Image` (background), `Button`, `CanvasGroup`
3. Add Component → `CardDragHandle`
4. Wire `CardDragHandle._rootCanvas` → the root Canvas of the Gameplay scene (or leave null to find at runtime)
5. Add a child `TMP_Text` for the card name
6. Set preferred size (e.g. width=120, height=160)
7. Save as prefab to `Assets/_Game/Features/Gameplay/UI/Component/CardSlot_Hand.prefab`
8. Assign this prefab to `HandPanel._cardSlotPrefab`

**Important:** The `CanvasGroup` is required for `CardDragHandle` to work (it manages `blocksRaycasts` during drag).

---

## 5. Champion Card in Fusion (F3.5)

The Champion card should always be available in the FusionPanel's base slot pool — it is never consumed from hand.

**Implementation approach (no code change needed):**
- The `FusionPanel.StageBase(cardId)` method accepts any card ID. The Champion's card ID comes from the deck data loaded during StartPhase.
- In the scene, add a dedicated "Champion Button" in the `PhaseInteractionPanel_Fusion` prefab near the UnitSlot area that always shows the player's champion.
- Wire this button's `OnClick` to call a small helper script that reads the champion ID from the DeckChoose selection and calls `FusionPanel.StageBase(championId)`.

**Alternatively**, you can add a dedicated button in the prefab:
1. Create a child button named `ChampionButton` under the same panel as `UnitSlot`
2. At runtime, `FusionPanel` (or a small companion script) will read the selected champion from `IGameplayDeckChooseSubsystem` and auto-populate this button
3. Clicking it calls `_fusion.StageBase(championCardId)`

This logic can be added later as a follow-up — the core fusion staging flow works without it (player can still drag the champion card from hand if it appears there, or the controller can auto-populate it).

---

## 6. PanelVisibilityRouter Integration

The existing `PanelVisibilityRouter.cs` (already in the project) handles showing/hiding phase panels.

In the Gameplay scene:
1. Find or create the `PanelVisibilityRouter` GameObject
2. In the `_phasePanels` array, add an entry:
   - Phase: `MainPhase`
   - Panel: Reference to the `PhaseInteractionPanel_Fusion` root GameObject

**Note:** `HandPanel` also self-manages visibility in `OnPhaseChanged()` (shows during `MainPhase` and `CombatPhase`). If you prefer unified control, remove the self-management from `HandPanel` and add it to `PanelVisibilityRouter` instead. But since HandPanel is wrapped in a drawer (HandPanelAnchor), it's typically always present and just opened/closed via the toggle.

---

## 7. Scene Hierarchy Recommendation

```
Gameplay Scene
├── SceneContext (GameplayInstaller)
├── Canvas_Gameplay
│   ├── Layout_Fullscreen_Gameplay (GameplayHUDController)
│   │   ├── Profile_Player
│   │   ├── Profile_Enemy1
│   │   ├── Profile_Enemy2 (inactive)
│   │   ├── PhaseNamePanel
│   │   └── ... (other HUD elements)
│   ├── PanelVisibilityRouter (routes phase → panel visibility)
│   ├── PhaseInteractionPanel_DeckChoose (GameplayDeckChoosePanel)
│   ├── PhaseInteractionPanel_Fusion (FusionPanel)    ← MainPhase
│   ├── PhaseInteractionPanel_DrawCard                ← DrawPhase
│   ├── HandPanelAnchor (PanelDrawer)                 ← Always accessible (Main+Combat)
│   │   └── PhaseInteractionPanel_Hand (HandPanel)
│   ├── SkillPanelAnchor (PanelDrawer)                ← CombatPhase
│   │   └── PhaseInteractionPanel_Skill
│   ├── TurnOrderPanelAnchor (PanelDrawer)            ← CombatPhase
│   │   └── PhaseInteractionPanel_TurnOrder
│   ├── PhaseInteractionPanel_MatchResult             ← GameOver
│   └── Overlay_Gameplay_Decks (GameplayDeckSelectOverlay)
└── ... (board, network objects, etc.)
```

---

## 8. Testing Checklist

- [ ] HandPanel shows cards from `IPlayerCardZoneSubsystem.HandChanged` during MainPhase
- [ ] Cards display correct names via `ICardLoadingManagerSubsystem.TryGetCardData`
- [ ] CardDragHandle allows dragging card slots
- [ ] Dropping a card onto `UnitSlot` → calls `FusionPanel.StageBase(cardId)`
- [ ] Dropping a card onto `FuseSlot1..4` → calls `FusionPanel.StageEquipSpell(index, cardId)`
- [ ] FusionPanel shows staging state from `IFusionSubsystem.StagingChanged`
- [ ] Clear button on fuse slots calls `IFusionSubsystem.ClearSlot(index)`
- [ ] Confirm button calls `IFusionSubsystem.ConfirmFusion()` and disables further input
- [ ] NormalAttackSlot and MovementSlot appear when a base card is staged
- [ ] Innate skill auto-occupies one fuse slot (only 3 remaining slots available)
- [ ] FusionPanel hides itself when phase changes away from MainPhase
- [ ] HandPanel is accessible via drawer toggle during MainPhase and CombatPhase
