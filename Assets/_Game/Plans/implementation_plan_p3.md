# Implementation Plan — Part 3: Gameplay, Execution Order, Editor Wiring, Verification

---

## 7. GAMEPLAY SCENE SUBSYSTEMS (SceneContext on Gameplay scene)

Bound in `GameplayInstaller` 🆕 on Gameplay scene's SceneContext.

> [!IMPORTANT]
> Gameplay models are the **Networked Models**. They derive from `NetworkBehaviour` with `[Networked]` properties. Photon Fusion syncs them. Only the State Authority controller modifies them. All clients observe via `OnChanged` callbacks (Fusion's `ChangeDetector` or `OnChangedRender`).

---

### 7.1 GameState Subsystem 🆕 (Central match orchestrator)

**Model (NetworkBehaviour):**
```
[Networked] GamePhase CurrentPhase    // enum: Start, Main, Combat, Draw, Result
[Networked] int       CurrentTurn
[Networked] PlayerRef ActivePlayer
[Networked] int       TurnTimer
[Networked] NetworkArray<PlayerData> Players  // HP, Mana, etc.
```

**Controller:**
```
void StartMatch()
void AdvancePhase()
void SkipTurn()
void EndMatch(PlayerRef winner)
```

**Views observing this:** All gameplay phase panels read `CurrentPhase` to show/hide themselves.

---

### 7.2 Hand Subsystem 🆕

**Model:**
```
[Networked] NetworkLinkedList<CardRef> CardsInHand
[Networked] int MaxHandSize
```

**Controller:**
```
void DrawCard()
void PlayCard(CardRef card, int targetSlot)
void DiscardCard(CardRef card)
```

**View = Hand Panel** (`PhaseInteractionPanel_Hand`)

---

### 7.3 FusePhase Subsystem 🆕 (Main Phase)

**Model:**
```
Observable<CardRef> SelectedUnit          // local only
Observable<CardRef> SelectedModifier      // local only (spell/equip)
Observable<bool>    IsReady
```

**Controller:**
```
void SelectUnit(CardRef)
void SelectModifier(CardRef)
void ConfirmFuse()                        → validates mana, executes fuse, updates Hand
void SetReady()
void SkipTurn()
```

**View = Fusion Panel** (`PhaseInteractionPanel_Fusion`)

---

### 7.4 Board Subsystem 🆕

**Model (NetworkBehaviour):**
```
[Networked] NetworkArray<TileState> Tiles  // occupant, effects
[Networked] NetworkLinkedList<UnitState> Units
```

**Controller:**
```
void PlaceUnit(UnitState unit, HexCoord pos)
void MoveUnit(int unitId, HexCoord target)
void RemoveUnit(int unitId)
HexCoord[] GetValidMoves(int unitId)
HexCoord[] GetAttackTargets(int unitId, PatternSO pattern)
```

**View = Board View (3D GameObject)** — MonoBehaviour on hex grid root.

---

### 7.5 Combat Subsystem 🆕

**Model (NetworkBehaviour):**
```
[Networked] NetworkLinkedList<int> TurnOrder    // unit IDs sorted by speed
[Networked] int ActiveUnitId
[Networked] CombatAction LastAction
```

**Controller:**
```
void ExecuteMove(int unitId, HexCoord target)
void ExecuteAttack(int unitId, int targetId, int skillIndex)
void ExecuteSkill(int unitId, int skillIndex, HexCoord target)
void NextUnit()
void EndCombatPhase()
```

**Views:**
- `PhaseInteractionPanel_Skill` → skill selection
- `PhaseInteractionPanel_TurnOrder` → turn order bar

---

### 7.6 DrawPhase Subsystem 🆕

**Model:**
```
Observable<List<CardRef>> DrawnCards       // local display
```

**Controller:**
```
void DrawCards(int count)
void ConfirmDraw()                         → add to hand
```

**View** = `PhaseInteractionPanel_DrawCard`

---

### 7.7 MatchResult Subsystem 🆕

**Model:**
```
Observable<bool>   IsWinner
Observable<int>    XpReceived
Observable<int>    GoldReceived
Observable<string> OpponentName
```

**View** = `PhaseInteractionPanel_MatchResult`

---

## 8. DI CONTEXT HIERARCHY

```
ProjectContext (DontDestroyOnLoad)
├── CoreInstaller
│   ├── UIManagerSubsystem (singleton)
│   ├── SceneLoaderSubsystem (singleton)
│   ├── AudioManagerSubsystem (singleton)      🆕
│   ├── AuthSessionSubsystem (singleton)       🆕
│   └── HttpServiceSubsystem (singleton)       🆕
│
├── Account Scene → SceneContext
│   └── AccountInstaller
│       ├── AccountLoginSubsystem (singleton within scene)
│       └── AccountRegisterSubsystem (singleton within scene)
│
├── Lobby Scene → SceneContext
│   └── LobbyInstaller
│       ├── LobbyMainSubsystem
│       ├── ProfileSubsystem
│       ├── BattleSubsystem (BattleSetup)
│       ├── MatchMakingSubsystem              🆕
│       ├── DeckSubsystem (DeckList)
│       ├── ShopSubsystem
│       ├── SettingSubsystem
│       └── MatchHistorySubsystem
│   └── GameObjectContext (on DeckBuild prefab)
│       └── DeckBuildSubsystem                 🆕
│   └── GameObjectContext (on CardDetail prefab)
│       └── CardDetailSubsystem                🆕
│
└── Gameplay Scene → SceneContext
    └── GameplayInstaller                      🆕
        ├── GameStateSubsystem
        ├── HandSubsystem
        ├── FusePhaseSubsystem
        ├── BoardSubsystem
        ├── CombatSubsystem
        ├── DrawPhaseSubsystem
        └── MatchResultSubsystem
```

---

## 9. STEP-BY-STEP EXECUTION ORDER

### Phase A: Foundation Fixes (Bugs + Core)

| # | Task |
|---|------|
| A1 | Fix BUG-5: Remove `using UnityEditor` from Bootstrapper.cs |
| A2 | Fix BUG-6: Add `: IController` to `ISceneLoaderController` |
| A3 | Fix BUG-3: Add `IDisposable` to all controller interfaces. Remove redundant declarations from concrete classes |
| A4 | Fix BUG-7: Move `RegisterPanel` from `Start()` to `OnEnable()` with guard in UIPanel.cs |
| A5 | Update `Observable` usage: ensure all model properties use `Observable<T>` (not plain auto-props) |
| A6 | Normalize UIManager API (rename `ShowScreen<T>` to `Show<T>`, etc.) |

### Phase B: Global Subsystems

| # | Task |
|---|------|
| B1 | Create `HttpService` subsystem (7 files in `Core/Scripts/UI/SubSystem/HttpService/`) |
| B2 | Create `AuthSession` subsystem (7 files in `Core/Scripts/UI/SubSystem/AuthSession/`) |
| B3 | Create `AudioManager` subsystem (7 files in `Core/Scripts/UI/SubSystem/AudioManager/`) |
| B4 | Update `CoreInstaller` to bind HttpService, AuthSession, and AudioManager |
| B5 | Update `Core.asmdef` if needed |

### Phase C: Account Scene

| # | Task |
|---|------|
| C1 | Refactor `AccountLoginModel` → Observable fields + ErrorMessage + IsSubmitting |
| C2 | Refactor `AccountLoginController` → remove navigation, add API call via HttpService |
| C3 | Refactor `AccountLoginSubsystem` → wire model events |
| C4 | Refactor `AccountLoginPanel` → listen to error/submitting events, move navigation to View |
| C5 | Repeat C1-C4 for AccountRegister |
| C6 | Update `AccountInstaller` if interface names changed |

### Phase D: Lobby Scene

| # | Task |
|---|------|
| D1 | Refactor `LobbyMainModel` → Observable user info fields |
| D2 | Refactor `LobbyMainController` → remove navigation methods (keep Logout), add Initialize fetch via HttpService |
| D3 | Refactor `LobbyMainSubsystem` → wire model events |
| D4 | Refactor `LobbyMainPanel` → direct UIManager calls for navigation, listen to model events |
| D5 | Expand ProfileSubsystem (model + controller + view) |
| D6 | Expand BattleSubsystem → BattleSetup with full model |
| D7 | Create MatchMakingSubsystem (7 files in `Features/Lobby/Scripts/MatchMaking/`) |
| D8 | Create MatchMakingPanel view, attach to `Overlay_Lobby_MatchMaking` prefab |
| D9 | Expand DeckSubsystem for DeckList |
| D10 | Create DeckBuildSubsystem (7 files + GameObjectContext installer in `Features/Lobby/Scripts/DeckBuild/`) |
| D11 | Create DeckBuildPanel view, attach to `Screen_Lobby_DeckBuild` prefab |
| D12 | Expand ShopSubsystem |
| D13 | Expand SettingSubsystem (Observable volumes, PlayerPrefs) |
| D14 | Expand MatchHistorySubsystem |
| D15 | Create CardDetailSubsystem (7 files + GameObjectContext) |
| D16 | Create popup views: `DeckItemContextPopup`, `ShopItemContextPopup`, `ConfirmationPopup`, `TextInputPopup` |
| D17 | Update `LobbyInstaller` with new bindings (MatchMaking) |
| D18 | Add new UIIdentifiers to Constants.cs for CardDetail, popups |
| D19 | Update `UIMappingSO` asset in editor (add new mappings) |

### Phase E: Gameplay Scene

| # | Task |
|---|------|
| E1 | Create `GameplayInstaller` MonoInstaller |
| E2 | Create GameStateSubsystem (NetworkBehaviour model) |
| E3 | Create HandSubsystem |
| E4 | Create FusePhaseSubsystem |
| E5 | Create BoardSubsystem (NetworkBehaviour model + hex grid logic) |
| E6 | Create CombatSubsystem |
| E7 | Create DrawPhaseSubsystem |
| E8 | Create MatchResultSubsystem |
| E9 | Create views for each gameplay panel prefab |
| E10 | Wire `SceneContext` + `GameplayInstaller` in Gameplay scene |
| E11 | Add Gameplay scene default UI mapping in `UIMappingSO` |

### Phase F: Photon Fusion Integration

| # | Task |
|---|------|
| F1 | Add Fusion `NetworkRunner` management (startup, shutdown, join/create) in BattleSetup/MatchMaking controllers |
| F2 | Convert gameplay models to `NetworkBehaviour` with `[Networked]` properties |
| F3 | Use `ChangeDetector` in subsystems to bridge Fusion → Observable events |
| F4 | Implement State Authority checks in gameplay controllers |
| F5 | Add `NetworkObject` to gameplay prefab hierarchy |

---

## 10. UNITY EDITOR WIRING — STEP BY STEP

### 10.1 ProjectContext Setup

1. Menu → Zenject → Create ProjectContext
2. ProjectContext prefab → Add `CoreInstaller` component
3. Drag `UIMappingSO` asset → `CoreInstaller.UIMapping` field
4. Ensure ProjectContext is at `Assets/Resources/ProjectContext.prefab`

### 10.2 Per-Scene Setup (Account, Lobby, Gameplay)

For each scene:
1. Create empty GameObject named `SceneContext`
2. Add `SceneContext` component
3. Add the scene's installer (`AccountInstaller`, `LobbyInstaller`, `GameplayInstaller`)
4. Under Installers list → drag the installer component
5. Create UIRoot hierarchy:
   ```
   Canvas (Screen Space - Overlay)
   └── UIRoot (add UIRoot component)
       ├── ScreenLayer (empty)
       ├── HUDLayer (empty)
       ├── PopupLayer (empty)
       ├── OverlayLayer (empty)
       └── SystemLayer (empty)
   ```
6. UIRoot component → populate `_layerParents`:
   - SCREEN → ScreenLayer transform
   - HUD → HUDLayer transform
   - POPUP → PopupLayer transform
   - OVERLAY → OverlayLayer transform
   - SYSTEM → SystemLayer transform

### 10.3 GameObjectContext for Scoped Subsystems

For `Screen_Lobby_DeckBuild` prefab:
1. Add `GameObjectContext` component to root
2. Create `DeckBuildInstaller : MonoInstaller` script
3. Add `DeckBuildInstaller` to the prefab root
4. In GameObjectContext → Installers → drag `DeckBuildInstaller`
5. DeckBuildInstaller binds: DeckBuildModel, DeckBuildController, DeckBuildSubsystem

Same for `Overlay_Lobby_CardDetails`.

### 10.4 Prefab View Attachment

For each prefab in the mapping table (Section 6):
1. Open prefab
2. Add View script to root GameObject (e.g., `AccountLoginPanel`)
3. Wire `[SerializeField]` fields by dragging child elements:
   - Buttons → Button components
   - Input fields → TMP_InputField components
   - Display texts → TMP_Text components
   - Sliders → Slider components
4. Set `_identifier` enum in inspector
5. Set `_layer` enum in inspector
6. Set `_isModal` checkbox (true for popups/overlays that start hidden)

### 10.5 UIMappingSO Asset Update

In Unity Editor:
1. Select `Assets/_Game/SOs/UIMappingSO`
2. Add entries for any new prefabs:
   - MatchMaking panel (306)
   - CardDetail overlay (add ID)
   - DeckItemContext popup (add ID)
   - ShopItemContext popup (add ID)
   - Confirmation popup (add ID)
   - TextInput popup (add ID)
3. Each entry: set UIIdentifier enum + drag prefab reference

---

## 11. FILES TO CREATE PER NEW SUBSYSTEM (Template)

Each subsystem = 7 files minimum:
```
Features/{Feature}/Scripts/{SubsystemName}/
├── I{Name}Model.cs
├── I{Name}Controller.cs
├── I{Name}Subsystem.cs
├── {Name}Model.cs
├── {Name}Controller.cs
├── {Name}Subsystem.cs
└── {Name}Panel.cs (or {Name}View.cs for GameObjects)
```

For GameObjectContext scoped subsystems, add:
```
Features/{Feature}/DI/
└── {Name}Installer.cs
```

---

## 12. ASSEMBLY DEFINITION UPDATES

| asmdef | Add Reference |
|--------|--------------|
| `Core.asmdef` | Fusion Runtime (GUID) — if Fusion types used in interfaces |
| `LobbyFeatures.asmdef` | Already has Core + Zenject. ✅ |
| `GameplayFeatures.asmdef` | Add Core, Zenject, Fusion Runtime, Fusion Sockets |
| `AccountFeatures.asmdef` | Already has Core + Zenject. ✅ |

---

## 13. VERIFICATION PLAN

### Automated
```
# Verify compilation
Unity Editor → open project → check Console for 0 errors
```

### Manual Flow Tests

| Test | Steps | Expected |
|------|-------|----------|
| Bootstrap → Account | Play Bootstrap scene | Loads Account scene, shows Login screen |
| Login → Lobby | Enter email/pass, click Login | API call via HttpService, loads Lobby, shows LobbyMain with user data |
| Register flow | Click Register on Login, fill form, submit | API call, loads Lobby |
| Register → Login | Click Back on Register | Shows Login screen (no duplicate panels) |
| Lobby navigation | Click each button (Profile, Battle, Deck, Shop, Settings) | Correct overlay/screen appears |
| Logout | Click Logout | AuthSession cleared, Account scene loads |
| Setting sliders | Adjust volume sliders | Audio volumes change immediately |
| Battle → MatchMaking | Configure battle, click Match | MatchMaking overlay appears |
| DeckList → DeckBuild | Select a deck | DeckBuild screen loads with GameObjectContext |

### Regression Checks
- No duplicate panels after navigation
- No `NullReferenceException` in console
- Panels properly cleanup on scene transition (Dispose called)
- Observable events fire correctly (add temporary Debug.Log in handlers)

---

## 14. SUMMARY — WHAT'S NEW vs WHAT EXISTS

| Category | Exists | New |
|----------|--------|-----|
| Global Subsystems | UIManager, SceneLoader | HttpService, AuthSession, AudioManager |
| Account Subsystems | Login, Register (skeleton) | Full Observable models, API integration |
| Lobby Subsystems | LobbyMain, Profile, Battle, Deck, Shop, Setting, MatchHistory (skeleton) | MatchMaking, DeckBuild, CardDetail + full expansion of all existing |
| Gameplay Subsystems | None | GameState, Hand, FusePhase, Board, Combat, DrawPhase, MatchResult |
| Views | Login, Register, LobbyMain, Profile, Battle, Deck, Shop, Setting, MatchHistory (partial) | All views fully wired. All popups. MatchMaking, DeckBuild, CardDetail new |
| Installers | CoreInstaller, AccountInstaller, LobbyInstaller | GameplayInstaller, DeckBuildInstaller, CardDetailInstaller |
| Bug Fixes | — | 8 bugs identified, fixes proposed |
