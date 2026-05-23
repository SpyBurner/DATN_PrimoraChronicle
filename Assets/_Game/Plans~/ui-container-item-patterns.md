# Gameplay UI — Container / Instantiated-Item Patterns

Each entry: script → container field → prefab field → components queried on item → data injected.

---

## HandPanel
**File:** `Features/Gameplay/Scripts/UI/HandPanel.cs`

| Field | Type | Role |
|---|---|---|
| `_cardSlotContainer` | `Transform` | Parent for spawned slots |
| `_cardSlotPrefab` | `GameObject` | Per-card slot |

**Components queried on spawned item:**
- `GetComponentInChildren<TMP_Text>()` — card name label
- `GetComponent<CardDragHandle>()` — drag state

**Data injected:**
- `cardData.name` (or raw `cardId`) → `TMP_Text.text`
- `cardId`, `handIndex` → `CardDragHandle.Initialize(cardId, handIndex)`

---

## SkillPanel
**File:** `Features/Gameplay/Scripts/UI/SkillPanel.cs`

| Field | Type | Role |
|---|---|---|
| `_skillSlotContainer` | `Transform` | Parent for spawned slots |
| `_skillSlotPrefab` | `GameObject` | Per-skill slot |

**Components queried on spawned item (by path):**
- `GetComponentInChildren<Button>()` — click trigger
- `transform.Find("NameText")?.GetComponent<TMP_Text>()` — skill name label
- `transform.Find("CooldownText")?.GetComponent<TMP_Text>()` — cooldown/status label
- `GetComponent<Image>()` — background tint

**Data injected:**
- `skillData.name` (or raw `skillId`) → `NameText.text`
- `"Used"` / `"CD: N"` / `""` → `CooldownText.text`
- `_readyColor` / `_cooldownColor` / `_disabledColor` → `Background.color`
- `() => OnSkillClicked(skillId)` → `Button.onClick`
- `Button.interactable` set based on ready state + local-turn flag

---

## TurnOrderPanel
**File:** `Features/Gameplay/Scripts/UI/TurnOrderPanel.cs`

| Field | Type | Role |
|---|---|---|
| `_content` | `Transform` | Parent for spawned items |
| `_turnOrderItemPrefab` | `GameObject` | Per-unit queue entry |

**Components queried on spawned item:**
- `GetComponentInChildren<TMP_Text>()` — unit name label
- `GetComponent<Image>()` — ownership / highlight background

**Data injected:**
- `cardData.name` (or raw `entry.CardId`) → `TMP_Text.text`
- `_localPlayerColor` / `_opponentColor` / `_currentActorHighlight` → `Image.color`

---

## FusionPanel — Fuse Slots
**File:** `Features/Gameplay/Scripts/UI/FusionPanel.cs`

| Field | Type | Role |
|---|---|---|
| `_fuseSlotContainer` | `Transform` | Parent for 4 fuse slots |
| `_fuseSlotPrefab` | `GameObject` | Per-slot fuse card drop target |

**Fixed count:** always 4 slots (built in `Awake`).

**Components queried on spawned item:**
- `GetComponent<FuseSlotUI>()` — typed component wrapper (see below)
- `GetComponent<FuseSlotDropTarget>()` — drop target

**Data injected:**
- `slotIndex`, `this (FusionPanel)` → `FuseSlotDropTarget.Initialize(i, panel)`
- `spellData.name` / `"Empty"` / `"Innate Skill"` → `FuseSlotUI.NameText.text`
- `FuseSlotUI.ClearButton.gameObject.SetActive(bool)` — show/hide clear button

**FuseSlotUI component fields** (`FuseSlotUI.cs`):
- `public TMP_Text NameText`
- `public Image Icon`
- `public Button ClearButton`

---

## DrawPhasePanel
**File:** `Features/Gameplay/Scripts/UI/DrawPhasePanel.cs`

| Field | Type | Role |
|---|---|---|
| `_cardSlotContainer` | `Transform` | Parent for spawned slots |
| `_cardSlotPrefab` | `GameObject` | Per-card selectable slot |

**Components queried on spawned item:**
- `GetComponentInChildren<TMP_Text>()` — card name label
- `GetComponent<Button>()` — keep/discard toggle
- `GetComponent<Image>()` — selection visual (updated separately in `UpdateSlotVisuals`)

**Data injected:**
- `cardData.name` (or raw `cardId`) → `TMP_Text.text`
- `() => ToggleCard(index)` → `Button.onClick`
- `_selectedColor` / `_discardedColor` → `Image.color` (post-spawn via `UpdateSlotVisuals`)

---

## GameplayDeckSelectOverlay
**File:** `Features/Gameplay/Scripts/UI/GameplayDeckSelectOverlay.cs`

| Field | Type | Role |
|---|---|---|
| `_deckSlot[]` | `GameObject[8]` | Fixed slot parents (pre-wired in Inspector) |
| `_deckButtonPrefab` | `DeckButton` | Typed prefab spawned into each slot |

**Pattern:** fixed-slot (not a single container). Each `_deckSlot[i].transform` is the parent.

**Components queried on spawned item:** none — prefab is already typed as `DeckButton`.

**Data injected:**
- `summary` (`DeckSummaryData`), `() => OnDeckClicked(summary)` → `DeckButton.Initialize(summary, callback)`

---

## Panels With No Dynamic Instantiation

| Script | Notes |
|---|---|
| `GameplayDeckChoosePanel` | Single pre-wired `DeckButton _currentDeckButton`; calls `Initialize(summary, callback)` on it. Delegates list rendering to `GameplayDeckSelectOverlay`. |
| `GameplayHUDController` | Two pre-wired `GameplayPlayerProfileUI` references; no spawning. |
| `MatchResultPanel` | All slots pre-wired in Inspector; no spawning. |
| `FusionPanel` (base slot) | `_unitSlot`, `_normalAttackSlot`, `_movementSlot` are all pre-wired; only fuse slots are dynamically spawned. |

---

## Summary: What Prefabs Must Provide

| Prefab field | Required component(s) |
|---|---|
| `_cardSlotPrefab` (Hand / DrawPhase) | `TMP_Text` (anywhere in children), `Button` (root), `Image` (root), `CardDragHandle` (root, Hand only) |
| `_skillSlotPrefab` | `Button` (any child), child named `"NameText"` with `TMP_Text`, child named `"CooldownText"` with `TMP_Text`, `Image` (root) |
| `_turnOrderItemPrefab` | `TMP_Text` (any child), `Image` (root) |
| `_fuseSlotPrefab` | `FuseSlotUI` (root), `FuseSlotDropTarget` (root) |
| `_deckButtonPrefab` | `DeckButton` component (root) — exposes `Initialize(DeckSummaryData, Action)` |
