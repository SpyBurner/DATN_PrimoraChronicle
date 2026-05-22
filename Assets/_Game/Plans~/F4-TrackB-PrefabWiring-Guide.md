# F4 Track B — Prefab Wiring Guide (Combat Phase UI)

This document explains how to manually wire the three F4 Track B scripts onto their respective prefabs.

---

## 1. TurnOrderPanel — `PhaseInteractionPanel_TurnOrder.prefab`

**Prefab path:** `Assets/_Game/Features/Gameplay/UI/Component/PhaseInteractionPanel_TurnOrder.prefab`

**Script path:** `Assets/_Game/Features/Gameplay/Scripts/UI/TurnOrderPanel.cs`

### Steps

1. Open the prefab in Prefab Mode (double-click).
2. Select the **root GameObject** of the prefab.
3. **Add Component** → `TurnOrderPanel`.
4. Wire serialized fields:

| Field | Target Object in Hierarchy | Notes |
|-------|---------------------------|-------|
| `_content` | `Panel/ScrollView_Horizontal/Viewport/Content` | The `RectTransform` container where turn-order items spawn. |
| `_turnOrderItemPrefab` | Create or assign a small prefab (see below) | A UI item with `Image` (BG) + `TMP_Text` child. |
| `_currentActorHighlight` | Default: `(1, 0.85, 0.2, 1)` yellow | Already set in code as default. |
| `_localPlayerColor` | Default: `(0.3, 0.7, 1, 1)` blue | Already set in code as default. |
| `_opponentColor` | Default: `(1, 0.4, 0.4, 1)` red | Already set in code as default. |

### TurnOrderItem prefab (create if missing)

Create a small UI prefab for individual turn-order entries:

```
TurnOrderItem (GameObject)
├── Image (component on root) — background/border
└── NameText (child TMP_Text) — unit display name
```

- Root: `RectTransform` (width ~80, height ~90), `Image` component (serves as BG).
- Child "NameText": `TextMeshProUGUI`, font size 12–14, centered.
- Save as prefab at `Assets/_Game/Features/Gameplay/UI/Component/TurnOrderItem.prefab`.
- Assign to `TurnOrderPanel._turnOrderItemPrefab`.

### PanelDrawer integration

This panel is drawer-wrapped by `TurnOrderPanelAnchor.prefab`:
- The `PhaseInteractionPanel_TurnOrder` is a **child** of `TurnOrderPanelAnchor`.
- `PanelDrawer` on the anchor references this panel's `RectTransform` as `_panel`.
- `Toggle_Sidebar` on the anchor controls the drawer via `PanelDrawer._toggle`.
- Use **Tools → Primora → Add PanelDrawers to Anchors** if not already wired.

### Pre-placed CardSlot_Empty items

The prefab has 5 pre-placed `CardSlot_Empty` children under `Content`. The script **destroys and recreates** items each time the queue changes, so those static items will be replaced at runtime. They serve as design-time placeholders only.

---

## 2. SkillPanel — `PhaseInteractionPanel_Skill.prefab`

**Prefab path:** `Assets/_Game/Features/Gameplay/UI/Component/PhaseInteractionPanel_Skill.prefab`

**Script path:** `Assets/_Game/Features/Gameplay/Scripts/UI/SkillPanel.cs`

### Steps

1. Open the prefab in Prefab Mode.
2. Select the **root GameObject**.
3. **Add Component** → `SkillPanel`.
4. Wire serialized fields:

| Field | Target Object in Hierarchy | Notes |
|-------|---------------------------|-------|
| `_skillSlotContainer` | `Panel (content area)` | The parent Transform where skill slot prefabs are spawned. |
| `_skillSlotPrefab` | Create or assign a skill slot prefab (see below) | |
| `_endTurnButton` | Add a Button (if not present) or use an existing "End Turn" button | Must be a `UnityEngine.UI.Button`. |
| `_actorNameText` | Add or find a `TMP_Text` element for showing current actor name | Optional — set null if not wanted. |
| `_readyColor` | Default: `(0.2, 0.8, 0.3, 1)` green | |
| `_cooldownColor` | Default: `(0.5, 0.5, 0.5, 0.7)` gray | |
| `_disabledColor` | Default: `(0.3, 0.3, 0.3, 0.5)` dark gray | |

### SkillSlot prefab (create if missing)

Create a UI prefab for individual skill buttons:

```
SkillSlot (GameObject)
├── Image (component on root) — background, colored by state
├── Button (component on root) — click to activate skill
├── NameText (child TMP_Text) — skill display name
└── CooldownText (child TMP_Text) — "CD: N" or "Used" or empty
```

- Root: `RectTransform` (width ~90, height ~110), `Image` + `Button` components.
- Child "NameText": `TextMeshProUGUI`, font size 12, top-aligned.
- Child "CooldownText": `TextMeshProUGUI`, font size 10, bottom-aligned, red color.
- Save as `Assets/_Game/Features/Gameplay/UI/Component/SkillSlot.prefab`.
- Assign to `SkillPanel._skillSlotPrefab`.

**Important:** The script finds children by name (`"NameText"`, `"CooldownText"`) using `transform.Find(...)`, so those child names must match exactly.

### End Turn Button

If the prefab doesn't have an End Turn button:
1. Create a child Button GameObject named `Button_EndTurn`.
2. Add `Button` component + `Image` (BG) + child `TMP_Text` saying "End Turn".
3. Assign to `SkillPanel._endTurnButton`.

### PanelDrawer integration

Same pattern as TurnOrder:
- `PhaseInteractionPanel_Skill` is a child of `SkillPanelAnchor.prefab`.
- `PanelDrawer` on the anchor references this panel as `_panel`.
- `Toggle_Sidebar` drives drawer open/close.

---

## 3. TargetingOverlay — Scene GameObject (not a panel prefab)

**Script path:** `Assets/_Game/Features/Gameplay/Scripts/UI/TargetingOverlay.cs`

### Steps

1. In the **Gameplay scene**, create an empty GameObject named `TargetingOverlay`.
2. **Add Component** → `TargetingOverlay`.
3. Ensure the Gameplay scene's `SceneContext` will inject into it (place it under a `GameObjectContext` or ensure it's on a root-level GO that the SceneContext finds).
4. Wire serialized fields:

| Field | Target/Value | Notes |
|-------|-------------|-------|
| `_tileHighlightPrefab` | Create a highlight prefab (see below) | A flat quad/disc mesh with a semi-transparent material. |
| `_rangeColor` | Default: `(1, 0.92, 0.016, 0.6)` yellow | Matches LEGACY spec §7.5. |
| `_validTargetColor` | Default: `(0.2, 0.8, 0.2, 0.8)` green | |
| `_invalidTargetColor` | Default: `(1, 0.2, 0.2, 0.6)` red | |
| `_highlightYOffset` | `0.05` | Slight offset above tile to prevent Z-fighting. |
| `_tileLayerMask` | Set to include only the layer your tiles are on | If tiles are on "Default" layer, leave as `Everything`. |

### TileHighlight prefab (create)

A simple world-space marker placed on hex tiles:

```
TileHighlight (GameObject)
├── MeshFilter → Quad or Hexagonal flat mesh
├── MeshRenderer → uses a transparent/unlit material
└── Scale: approximately matches tile size (e.g., 1.5 × 1.5 × 1)
```

**Material setup:**
- Shader: `Unlit/Transparent` or `Universal Render Pipeline/Unlit` with surface type = Transparent.
- The script sets `renderer.material.color` at runtime, so the base color doesn't matter (white is fine).
- Save as `Assets/_Game/Features/Gameplay/UI/Component/TileHighlight.prefab` (or in a Prefabs folder).
- Assign to `TargetingOverlay._tileHighlightPrefab`.

### Zenject injection

The `TargetingOverlay` uses `[Inject]` attributes. For scene-level injection to work:
- The GameObject must be discoverable by the SceneContext. By default, all MonoBehaviours in the scene under the SceneContext hierarchy get injected.
- If placed outside the SceneContext hierarchy, add a `ZenAutoInjecter` component to the GameObject.

### Input flow

The overlay handles mouse input internally:
- **Hover**: Raycast from camera → resolve to hex → call `_targeting.HoverTile(coord)` → update highlight colors.
- **Left-click**: Raycast → validate target → call `_targeting.ConfirmTarget(coord)`.
- **Right-click / Escape**: call `_targeting.Cancel()`.

---

## 4. PanelVisibilityRouter — Phase Gating for Combat Panels

The existing `PanelVisibilityRouter.cs` (already in the scene) controls which panels are visible per phase. Add entries for the combat panels:

1. Select the `PanelVisibilityRouter` GameObject in the Gameplay scene.
2. In the `_phasePanels` array, add:

| Phase | Panel Reference |
|-------|-----------------|
| `CombatPhase` | `SkillPanelAnchor` (the anchor root, not the inner panel) |
| `CombatPhase` | `TurnOrderPanelAnchor` |

**Note:** The `PanelVisibilityRouter` shows/hides by `SetActive`. Since `SkillPanel` and `TurnOrderPanel` also gate themselves via `_gameState.PhaseChanged`, either approach works. Using `PanelVisibilityRouter` is preferred so all phase visibility is in one place — then you can remove the per-panel `OnPhaseChanged` gating if desired, or keep both for safety.

---

## 5. Summary Checklist

- [ ] `TurnOrderPanel` component added to `PhaseInteractionPanel_TurnOrder.prefab` root
- [ ] `_content` wired to `Content` RectTransform
- [ ] `_turnOrderItemPrefab` created and assigned
- [ ] `SkillPanel` component added to `PhaseInteractionPanel_Skill.prefab` root
- [ ] `_skillSlotContainer` wired to `Panel (content area)`
- [ ] `_skillSlotPrefab` created and assigned (children named `NameText`, `CooldownText`)
- [ ] `_endTurnButton` wired (create `Button_EndTurn` if needed)
- [ ] `TargetingOverlay` GameObject created in Gameplay scene
- [ ] `_tileHighlightPrefab` created and assigned (flat transparent mesh)
- [ ] `PanelVisibilityRouter._phasePanels` updated with CombatPhase entries
- [ ] Run **Tools → Primora → Add PanelDrawers to Anchors** to ensure drawer wiring is up to date
