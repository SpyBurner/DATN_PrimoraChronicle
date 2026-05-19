# Remaining Hand-Wiring Tasks (Unity Editor)

These tasks cannot be done in code alone and require working inside the Unity Editor.
All code prerequisites are committed on `feat/ClaudeAutomation`.

---

## 1. Wire `PhaseInteractionPanel_DeckChoose.prefab`

**File**: `Assets/_Game/Features/Gameplay/UI/Component/PhaseInteractionPanel_DeckChoose.prefab`

**Prefab structure** (verified via MCP inspection):
```
PhaseInteractionPanel_DeckChoose    ← root, add GameplayDeckChoosePanel here
├── Panel                           ← left column (VerticalLayoutGroup)
│   ├── PhaseNameText
│   ├── Panel                       ← timer row
│   │   └── TimerText (TMP_Text)   ← wire to _timerText
│   └── Button_Confirm              ← has Button component → _confirmButton
└── Panel (1)                       ← right column
    └── DeckButton (static)         ← KEEP — wire to _currentDeckButton
```

**Steps**:
1. Open the prefab in Prefab Edit mode.
2. Select the root `PhaseInteractionPanel_DeckChoose` GameObject.
3. Add Component → `GameplayDeckChoosePanel`.
4. Wire serialized fields:
   - `_currentDeckButton` → drag the static `DeckButton` child inside `Panel (1)`
   - `_deckSelectOverlay` → drag the `Overlay_Gameplay_Decks` instance in the scene
     *(set at scene level, not inside the prefab — leave slot empty in prefab, fill in scene)*
   - `_timerText` → drag the TMP_Text inside the timer row (`Panel > Panel > TimerText`)
   - `_confirmButton` → drag `Button_Confirm`
5. **Do NOT delete** the static `DeckButton` — it is now `_currentDeckButton` (current deck display).
6. Save the prefab.

---

## 2. Wire `Overlay_Gameplay_Decks.prefab`

**File**: `Assets/_Game/Features/Gameplay/UI/Component/Overlay_Gameplay_Decks.prefab`
**Script**: `GameplayDeckSelectOverlay`

This prefab is a clone of the Lobby DeckPanel (8 deck slots + DeckButton prefab reference).
It shows the deck list when the player taps the current-deck button; clicking a deck closes it
and fires `DeckSelected` back to `GameplayDeckChoosePanel`.

**Steps**:
1. Open the prefab in Prefab Edit mode.
2. Select the root GameObject.
3. Add Component → `GameplayDeckSelectOverlay`.
4. Wire serialized fields:
   - `_deckSlot[0..7]` → drag each of the 8 slot GameObjects (same as Lobby DeckPanel wiring)
   - `_deckButtonPrefab` → drag `Assets/_Game/Features/Lobby/UI/Component/DeckButton.prefab`
5. Set the root GameObject **inactive** by default (`SetActive(false)` in the Inspector checkbox).
6. Save the prefab.

---

## 3. Create `GameplayDeckChooseNetworkView` Prefab

**Script**: `Assets/_Game/Features/Gameplay/Scripts/DeckChoose/GameplayDeckChooseNetworkView.cs`

**Steps**:
1. In the Project window, right-click → Create → Empty Prefab. Name it
   `GameplayDeckChooseNetworkView`.
2. Open the prefab. Add these components to the root GameObject:
   - `Network Object` (Fusion) — this makes it a spawnable network prefab
   - `Game Object Context` (Zenject) — enables DI injection for `IGameplayDeckChooseSubsystem`
   - `GameplayDeckChooseNetworkView` script
3. On `Game Object Context`: add a `MonoInstaller` subcomponent (create an empty one if needed)
   so Zenject can resolve the Gameplay scene bindings into this spawned object.
4. **Register with Fusion**:
   - Open `Project Settings → Fusion → Network Object Prefab Table`
   - Add the new prefab.
5. **Wire `NetworkSpawner`**:
   - In the Gameplay scene, select the `NetworkSpawner` GameObject.
   - Assign the new prefab to the `Deck Choose View Prefab` (NetworkPrefabRef) field.

---

## 4. Wire `PhaseVisibilityController` in the Gameplay Scene

**Script**: `Assets/_Game/Features/Gameplay/Scripts/DeckChoose/PhaseVisibilityController.cs`

`PhaseVisibilityController` is a plain `MonoBehaviour`. Attach it to any persistent HUD root
GameObject in the Gameplay scene (e.g., the Canvas root or a dedicated HUD manager object).

**Serialized field — `_phasePanels` (array)**:

| Index | Phase | Panel |
|---|---|---|
| 0 | `StartPhase` | `PhaseInteractionPanel_DeckChoose` instance in scene |
| 1 | `MainPhase` | `PhaseInteractionPanel` (or hand/skill anchor root) |
| 2 | `CombatPhase` | *(leave Panel null to hide all when in combat)* |
| 3 | `DrawPhase` | `PhaseInteractionPanel_DrawCard` instance |

Add entries for every phase that needs a visible panel. Phases with no entry or a null Panel
simply leave those panels untouched by this controller (they stay at their last state).

**Wire `_deckSelectOverlay` on the panel instance**:
After attaching `GameplayDeckChoosePanel` to the prefab and placing the panel in the scene,
select the scene instance and wire `_deckSelectOverlay` → `Overlay_Gameplay_Decks` scene object.

---

## 5. Verify Gameplay SceneContext Installer Registration

Confirm `GameplayInstaller` is listed under the Gameplay scene's `SceneContext` bindings:
1. Open `Assets/_Game/Scenes/Gameplay.unity`.
2. Find the `SceneContext` GameObject.
3. Check that `GameplayInstaller` appears in the `Mono Installers` list.
   If not, add it.

`GameplayInstaller` now binds:
- `GameplayDeckSubsystem` → `IGameplayDeckSubsystem`
- `GameplayDeckChooseModel`
- `GameplayDeckChooseController`
- `GameplayDeckChooseSubsystem`

---

## Notes

- After completing all items above, run a local test session (both players in same room)
  and verify the full StartPhase → deck confirm → MainPhase flow.
- Timer text should count down from 30 s in `PhaseInteractionPanel_DeckChoose`.
- Clicking `_currentDeckButton` should open `Overlay_Gameplay_Decks`; clicking a deck in the
  overlay should close it and update the button label.
- Both players confirming (or timer expiring) should transition to `MainPhase`.
