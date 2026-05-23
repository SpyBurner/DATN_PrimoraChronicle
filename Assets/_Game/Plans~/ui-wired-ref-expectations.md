# Gameplay UI — Wired-Reference Expectations (non-container)

SerializeField refs that are **not** the container+prefab spawn pattern but still require a
specific component or hierarchy shape from the object dragged in.

Format: `Script._field (DeclaredType)` — what it calls / what the target must provide.

---

## Cross-component typed refs — specific API called on the wired object

### GameplayDeckChoosePanel → `_currentDeckButton` (`DeckButton`)
- **Calls:** `_currentDeckButton.Initialize(DeckSummaryData summary, Action onClickCallback)`
- **Must have:** `DeckButton` component with `Initialize(DeckSummaryData, Action)` method.

### GameplayDeckChoosePanel → `_deckSelectOverlay` (`GameplayDeckSelectOverlay`)
- **Subscribes:** `DeckSelected` event (`Action<DeckSummaryData>`)
- **Calls:** `gameObject.SetActive(false)` / `gameObject.SetActive(true)`
- **Must have:** `GameplayDeckSelectOverlay` component.

### GameplayHUDController → `_localProfile` / `_opponentProfile` (`GameplayPlayerProfileUI`)
- **Calls:** `profile.Bind(PlayerRef playerRef, bool isLocal)`
- **Must have:** `GameplayPlayerProfileUI` component.

### BaseSlotDropTarget → `_fusionPanel` (`FusionPanel`)
- **Calls:** `_fusionPanel.StageBase(string cardId)` — on drop of a `CardDragHandle`.
- **Must have:** `FusionPanel` component with `StageBase(string)`.

### FuseSlotDropTarget → `_fusionPanel` (`FusionPanel`)
- Same target type as above.
- **Calls:** `_fusionPanel.StageEquipSpell(int slotIndex, string cardId)` — on drop of a `CardDragHandle`.
- **Must have:** `FusionPanel` component with `StageEquipSpell(int, string)`.
- Note: also set via code — `Initialize(int slotIndex, FusionPanel panel)` is called by `FusionPanel.BuildFuseSlots()`.

### FusionPanel → `_handPanel` (`HandPanel`)
- Null-checked in `Awake` only; **no direct calls inside FusionPanel**.
- Must have: `HandPanel` component. The wiring exists for future drag-source lookup or to enforce that the hand panel is present in the scene.

---

## Typed prefab instantiated in world space (not a UI container)

### TargetingOverlay → `_tileHighlightPrefab` (`TileHighlight`)
- **Instantiates** into world space (3D), not into a UI container.
- **After spawn calls:** `highlight.SetColor(Color color)`
- **Must have on prefab:** `TileHighlight` component, which itself requires a `Renderer _renderer`
  (set via Inspector on the prefab) — `SetColor` does `_renderer.material.color = color`.

---

## Hierarchy/structural expectations (not a component API, but a named child)

### PanelDrawer — `"OpenPosition"` child
- `_panel` (`RectTransform`): the rect that slides. Uses `.anchoredPosition` for DOTween animation.
- `_toggle` (`Toggle`): subscribes to `onValueChanged`.
- **Implicit child requirement:** the anchor prefab (or its parent) must have a direct child
  named exactly `"OpenPosition"` with a `RectTransform`. `PanelDrawer.Awake` reads it via
  `transform.Find("OpenPosition")` and casts it to `(RectTransform)` to get `_openPosition`.
  Missing or misnamed → silent fallback to `Vector2.zero` (panel never opens).

---

## Simple GameObject visibility refs (SetActive only)

These are wired GameObjects where the only call is `SetActive(bool)`. No specific component
is queried; they just need to exist as GameObjects.

| Script | Field | Calls |
|---|---|---|
| `FusionPanel` | `_normalAttackSlot` (`GameObject`) | `SetActive(bool)` — shown when base card is set |
| `FusionPanel` | `_movementSlot` (`GameObject`) | `SetActive(bool)` — shown when base card is set |
| `FusionPanel` | `_unitSlot` (`Transform`) | `gameObject.SetActive(bool)` — wraps unit display area |
| `GameplayHUDController` | `_enemy2ProfileRoot` (`GameObject`) | `SetActive(false)` on enable (placeholder for 3-player) |
| `PanelVisibilityRouter` | `_phasePanels[].Panel` (`GameObject`) | `SetActive(bool)` per phase change |
