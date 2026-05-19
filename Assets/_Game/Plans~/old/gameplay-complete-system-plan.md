# Gameplay — Complete Standalone Multiplayer System Plan

## Context

The Gameplay scene needs to function as a self-contained multiplayer game: two players connect via Photon Fusion, select decks, assemble units, fight on the hex board across repeating Main→Combat→Draw cycles, and see a match result. The networking backbone (NetworkGameplayManager, NetworkUnit, NetworkPlayerState, skill SO library) is largely functional but suffers from brittle singleton lookups (`FindObjectsByType`, hardcoded fallbacks), an incomplete DI layer, and zero UI for most phases (Fusion, Draw, Skills, Match Result). This plan defines every required component, evaluates the full rulebook as isolated features, maps each to a component, and splits work into two parallel tracks with independent test paths.

---

## 1. Core Subsystem Inputs

These are already bound in `CoreInstaller` (ProjectContext) and available to Gameplay by injection.

| Subsystem | Gameplay Usage |
|---|---|
| `IAuthSessionSubsystem` / `IAuthSessionModel` | Local userId + auth token for HTTP calls |
| `ICardLoadingManagerSubsystem` | Resolve card string IDs → `CardData`, `SkillData`, `StatusEffectData` |
| `INetworkManagerSubsystem` | Fusion session lifecycle, `PlayerJoined`/`PlayerLeft` events |
| `IBackendBridgeSubsystem` | Receives `StartSessionCommand`; reports `MatchResultData` on game over |
| `IHttpServiceSubsystem` | Fetches `DeckDetailData` during StartPhase |
| `ISceneLoaderSubsystem` | Returns to Lobby on match end |
| `IDebugLogger` | Logging |

### Profile Subsystem Migration

`IProfileSubsystem` is currently bound in `LobbyInstaller` (Lobby SceneContext). Move the **binding only** to `CoreInstaller` (ProjectContext) so the Gameplay HUD can inject it. Interface file path stays at `Core/Scripts/Interfaces/Features/Lobby/Profile/`.

---

## 2. Rulebook → Feature Map

Each rule from the rulebook maps to a feature with owning track.

| # | Feature | Rulebook §| Track A (Engine) | Track B (UI) |
|---|---|---|---|---|
| 1 | Hex Board — grid, deploy area, boundary | §1 | `IBoardSubsystem` rewrite | tile highlight visuals |
| 2 | Cards & Deck — Champion + 20, granted cards | §3 | `NetworkPlayerState` rewrite | — |
| 3 | Fusion — 4 slots, troop + equips, deploy to Deploy Area | §4 | `FusionSubsystem` (new) | `FusionPanel` (new) |
| 4 | Start Phase — deck select, 6-card draw | §5 | Complete existing `GameplayDeckChoose*` | Complete `GameplayDeckChoosePanel` |
| 5 | Main Phase — deploy 1 unit + play spells | §5 | `IMainPhaseSubsystem` (thin), spell dispatch | `HandPanel` (new), `FusionPanel` |
| 6 | Combat Phase — speed queue, unit turns, move + action | §5 | `NetworkGameplayManager` rewrite, `ICombatSubsystem` | `SkillPanel`, `TurnOrderPanel` (new) |
| 7 | Death & Death Anchor | §5 | In `NetworkUnit`, fires via `ICombatSubsystem` | HP bar update |
| 8 | Board Clear | §5 | In `NetworkGameplayManager` | — |
| 9 | Draw Phase — draw 2, keep ≤6, discard rest | §5 | `DrawPhaseSubsystem` (new) | `DrawPhasePanel` (new) |
| 10 | Win Condition — last alive or HP tiebreak at 1h | §5 | `MatchResultSubsystem` (new) | `MatchResultPanel` (new) |
| 11 | Persistent Units — survive board clear | §6 | `NetworkUnit.IsPersistent` flag handling | — |
| 12 | Evolution Chain — 4 growth stacks | §7 | In `GenericSkillBehaviorSO` / `NetworkUnit` | visual unit swap |
| 13 | Damage Pipeline — 3-pass | §8 | `NetworkUnit` rewrite (`AggregatePass`/`InterceptPass`/`CommitPass`) | — |
| 14 | Targeting — tile-based, bitmask `target_condition` | §9 | Validated in `NetworkGameplayManager` | tile highlight in `LocalInteractionController` |
| 15 | Tile Effects — lingering, one-per-tile, replacement | §10 | `NetworkTileEffect` rewrite | visual effect on tile |
| 16 | Stat Modification — MaxHP delta mirrors current HP | §11 | In `NetworkUnit.ModifyMaxHP()` | — |
| 17 | Skill Cooldowns — tick at turn start, one_time flag | §12 | In `NetworkUnit.StartTurn()` | cooldown display in `SkillPanel` |
| 18 | Hand & Deck Management — max 6, reshuffle | §13 | `NetworkPlayerState` + `IHandSubsystem` | `HandPanel`, `DrawPhasePanel` |
| 19 | Behavior System — SO-mapped behaviors | §14 | `GenericSkillBehaviorSO` refactor | — |
| 20 | Display Pattern — preview AOE on target hover | §15 | Validated in skill SO | `LocalInteractionController` highlight |

---

## 3. Existing Component Decisions

| Component | Decision | Reason |
|---|---|---|
| `NetworkGameplayManager` | **Rewrite** | `FindObjectsByType` singleton; monolithic phase logic; hardcoded default decks |
| `NetworkPlayerState` | **Rewrite** | Needs `ICardLoadingManagerSubsystem` integration; clean deck init from `DeckDetailData` |
| `NetworkUnit` | **Rewrite** | `FindFirstObjectByType<BoardManager>` calls; damage pipeline lacks named pass structure |
| `BoardManager` | **Rewrite** | Not DI-injectable; needs to implement `IBoardSubsystem` |
| `HexTile` | **Keep** | Clean; minor fix — register with board via `Spawned()` callback |
| `NetworkTileEffect` | **Rewrite** | Direct `BoardManager` lookup; needs clean registration path |
| `NetworkSpawner` | **Rewrite** | Uses `FindObjectByType<SceneContext>` for DI; fragile |
| `GenericSkillBehaviorSO` | **Keep, Refactor** | 23 skills work; replace `FindObjectsByType` in `Execute()` with passed references |
| `SkillBehaviorSO` | **Keep** | Clean base class |
| `ParanoidMinimaxAI` | **Keep, Refactor** | Works; accept injected board/manager references instead of static lookups |
| `GameplayDeckChoose*` (all 4) | **Keep, Complete** | Full MVC subsystem stack exists; needs installer wiring, interface migration, prefab setup |
| `GameplayDeckSubsystem` | **Keep** | Works; minor cleanup |
| `PhaseVisibilityController` | **Rewrite** | Must subscribe to `IGameStateSubsystem.PhaseChanged` event, not poll `Update()` |
| `LocalInteractionController` | **Rewrite** | Must inject `IBoardSubsystem` and `ICombatSubsystem`; not self-managing |
| `PanelDrawer` | **Keep** | Clean utility |
| `GameplayHUDController` | **Rewrite** | Auto-discovers GameObjects by name; must inject subsystems |
| `GameplayPlayerProfileUI` | **Rewrite** | Must inject `IProfileSubsystem` |
| `GameplayDeckChoosePanel` | **Keep, Complete** | Mostly done; wire prefab references |
| `GameplayDeckSelectOverlay` | **Keep** | Clean |
| `AddPanelDrawersEditor` | **Keep** | Useful editor tool |

---

## 4. New Subsystem Interfaces

All new interfaces go in `Core/Scripts/Interfaces/Features/Gameplay/`:

```
Core/Scripts/Interfaces/Features/Gameplay/
├── StartPhase/                          ← move existing DeckChoose files here
│   ├── IGameplayDeckChooseSubsystem.cs
│   ├── IGameplayDeckChooseController.cs
│   ├── IGameplayDeckChooseModel.cs
│   ├── IGameplayDeckChooseNetworkBridge.cs
│   ├── GameplayDeckChooseStateData.cs
│   └── IGameplayDeckSubsystem.cs
├── GameState/
│   ├── IGameStateSubsystem.cs           ← PhaseChanged, RoundChanged, TimerChanged events
│   ├── IGameStateController.cs
│   ├── IGameStateModel.cs
│   └── GameStateData.cs                 ← { GameplayPhase Phase, int Round, float Timer, int[] PlayerHPs }
├── Board/
│   ├── IBoardSubsystem.cs               ← FindTile, GetAllUnits, GetUnitAt, UnitMoved event
│   ├── IBoardController.cs
│   └── IBoardModel.cs
├── Combat/
│   ├── ICombatSubsystem.cs              ← TurnStarted/Ended/DamageDealt; RequestAttack/Skill/EndTurn
│   ├── ICombatController.cs
│   └── ICombatModel.cs
├── FusePhase/
│   ├── IFusionSubsystem.cs              ← StageTroop, StageEquip, DeployUnit; slot state events
│   ├── IFusionController.cs
│   └── IFusionModel.cs
├── DrawPhase/
│   ├── IDrawPhaseSubsystem.cs           ← NewCardsChanged, HandChanged; ConfirmDraw
│   ├── IDrawPhaseController.cs
│   └── IDrawPhaseModel.cs
├── Hand/
│   ├── IHandSubsystem.cs                ← HandChanged, HandCountChanged (read-only view)
│   └── IHandModel.cs
└── GameOver/
    ├── IMatchResultSubsystem.cs         ← MatchResultReceived event
    └── MatchResultData.cs               ← { WinnerUserId, LoserUserId, XpGained, GoldGained }
```

### Key Interface Signatures

```csharp
// IGameStateSubsystem
event UnityAction<GameplayPhase> PhaseChanged;
event UnityAction<int> RoundChanged;
event UnityAction<float> TimerChanged;
GameplayPhase CurrentPhase { get; }
int LocalPlayerIndex { get; }

// IBoardSubsystem
HexTile FindTile(int p, int q);
NetworkUnit GetUnitAt(int p, int q);
IReadOnlyList<NetworkUnit> GetAllUnits();
event UnityAction<NetworkUnit, int, int> UnitMoved;   // unit, newP, newQ
event UnityAction<NetworkUnit> UnitDied;

// ICombatSubsystem
event UnityAction<NetworkUnit> TurnStarted;
event UnityAction<NetworkUnit> TurnEnded;
event UnityAction<NetworkUnit, int> DamageDealt;      // target, amount
Task RequestNormalAttack(int targetP, int targetQ);
Task RequestSkillUse(int skillIndex, int targetP, int targetQ);
Task RequestEndTurn();
IReadOnlyList<NetworkUnit> GetCurrentQueue();

// IFusionSubsystem
event UnityAction<string> StagedTroopChanged;
event UnityAction<IReadOnlyList<string>> StagedEquipsChanged;
void StageTroop(string troopId);
void StageEquip(int slot, string equipId);
void ClearStaging();
Task DeployUnit();

// IDrawPhaseSubsystem
event UnityAction<IReadOnlyList<string>> NewCardsChanged;
void MoveToKeep(int newCardIndex);
void MoveToDiscard(int handIndex);
Task ConfirmDraw();
```

---

## 5. Implementation File Layout

```
Features/Gameplay/Scripts/
├── GameState/
│   ├── GameStateModel.cs
│   ├── GameStateController.cs
│   └── GameStateSubsystem.cs
├── Board/
│   ├── BoardModel.cs
│   ├── BoardController.cs
│   └── BoardSubsystem.cs
├── Combat/
│   ├── CombatModel.cs
│   ├── CombatController.cs
│   └── CombatSubsystem.cs
├── FusePhase/
│   ├── FusionModel.cs
│   ├── FusionController.cs
│   ├── FusionSubsystem.cs
│   └── FusionNetworkView.cs            ← bridges DeployUnit RPC → NetworkUnit spawn
├── DrawPhase/
│   ├── DrawPhaseModel.cs
│   ├── DrawPhaseController.cs
│   └── DrawPhaseSubsystem.cs
├── Hand/
│   ├── HandModel.cs
│   └── HandSubsystem.cs
├── GameOver/
│   ├── MatchResultModel.cs
│   ├── MatchResultController.cs
│   └── MatchResultSubsystem.cs
├── _NetworkMono/
│   ├── NetworkGameplayManager.cs       ← rewrite
│   ├── NetworkPlayerState.cs           ← rewrite
│   ├── NetworkUnit.cs                  ← rewrite
│   ├── NetworkTileEffect.cs            ← rewrite
│   ├── NetworkSpawner.cs               ← rewrite
│   ├── BoardManager.cs                 ← rewrite (implements IBoardSubsystem)
│   └── HexTile.cs                      ← keep
├── Combat/
│   └── [existing] ParanoidMinimaxAI.cs ← keep, refactor
├── DeckChoose/
│   └── [existing 4 files]              ← keep, complete
├── Interaction/
│   └── LocalInteractionController.cs   ← rewrite (inject IBoardSubsystem, ICombatSubsystem)
└── FusePhase/Skills/
    ├── [existing] SkillBehaviorSO.cs   ← keep
    └── [existing] GenericSkillBehaviorSO.cs ← keep, refactor Execute() signature
```

---

## 6. Track A — Game Engine

**Owner:** Team Member A
**Scope:** All NetworkBehaviour rewrites, new subsystem MVC stacks, DI installer, skill refactor
**Test path:** Lobby → "Play vs AI" → Gameplay (host). Game cycles phases to completion with 2 AI opponents. No UI required — verify via console logs and `IGameStateSubsystem` events.

### A1 — DI Setup (prerequisite)

- Move `IProfileSubsystem` binding from `LobbyInstaller` → `CoreInstaller`
- Update `GameplayInstaller` with all new bindings:
  ```csharp
  // Existing (already partially present — verify)
  Container.BindInterfacesAndSelfTo<GameplayDeckSubsystem>().AsSingle().NonLazy();
  Container.BindInterfacesAndSelfTo<GameplayDeckChooseModel>().AsSingle().NonLazy();
  Container.BindInterfacesAndSelfTo<GameplayDeckChooseController>().AsSingle().NonLazy();
  Container.BindInterfacesAndSelfTo<GameplayDeckChooseSubsystem>().AsSingle().NonLazy();
  // New
  Container.BindInterfacesAndSelfTo<GameStateModel>().AsSingle().NonLazy();
  Container.BindInterfacesAndSelfTo<GameStateController>().AsSingle().NonLazy();
  Container.BindInterfacesAndSelfTo<GameStateSubsystem>().AsSingle().NonLazy();
  Container.BindInterfacesAndSelfTo<BoardModel>().AsSingle().NonLazy();
  Container.BindInterfacesAndSelfTo<BoardController>().AsSingle().NonLazy();
  Container.BindInterfacesAndSelfTo<BoardSubsystem>().AsSingle().NonLazy();
  Container.BindInterfacesAndSelfTo<CombatModel>().AsSingle().NonLazy();
  Container.BindInterfacesAndSelfTo<CombatController>().AsSingle().NonLazy();
  Container.BindInterfacesAndSelfTo<CombatSubsystem>().AsSingle().NonLazy();
  Container.BindInterfacesAndSelfTo<FusionModel>().AsSingle().NonLazy();
  Container.BindInterfacesAndSelfTo<FusionController>().AsSingle().NonLazy();
  Container.BindInterfacesAndSelfTo<FusionSubsystem>().AsSingle().NonLazy();
  Container.BindInterfacesAndSelfTo<DrawPhaseModel>().AsSingle().NonLazy();
  Container.BindInterfacesAndSelfTo<DrawPhaseController>().AsSingle().NonLazy();
  Container.BindInterfacesAndSelfTo<DrawPhaseSubsystem>().AsSingle().NonLazy();
  Container.BindInterfacesAndSelfTo<HandModel>().AsSingle().NonLazy();
  Container.BindInterfacesAndSelfTo<HandSubsystem>().AsSingle().NonLazy();
  Container.BindInterfacesAndSelfTo<MatchResultModel>().AsSingle().NonLazy();
  Container.BindInterfacesAndSelfTo<MatchResultController>().AsSingle().NonLazy();
  Container.BindInterfacesAndSelfTo<MatchResultSubsystem>().AsSingle().NonLazy();
  ```
- Move DeckChoose interface files to `Core/Scripts/Interfaces/Features/Gameplay/StartPhase/`

### A2 — BoardManager → BoardSubsystem

- Implements `IBoardSubsystem` directly
- `HexTile.Spawned()` calls `boardSubsystem.RegisterTile(this)` via runner injection
- `FindTile(p,q)` replaces all direct dictionary access across other classes
- `ResolveCoordinateToPosition(p,q)` stays as-is (geometry utility)

### A3 — NetworkSpawner

- Inject `INetworkManagerSubsystem` for `PlayerJoined` event (no `FindObjectsByType`)
- Inject `IBoardSubsystem` for tile registration
- `SpawnPlayerPiece()` spawns `NetworkPlayerState` + `GameplayDeckChooseNetworkView` per player
- Read `ai_count` from `SessionInfo.Properties` to spawn AI players
- Create `GameplayDeckChooseNetworkView` prefab (NetworkObject + `GameObjectContext` + empty `MonoInstaller`)

### A4 — NetworkGameplayManager

- Inject `IBoardSubsystem`, `ICombatSubsystem`, `IGameStateSubsystem`, `IDebugLogger`
- Phase machine: explicit `Enter_/Exit_` method pairs per phase
- `IGameStateSubsystem` fires `PhaseChanged` on every transition
- `AutoConfirmDecks()` calls `IGameplayDeckChooseSubsystem.AutoConfirmLastDeck()` via bridge
- Board clear: destroy non-persistent units on Deploy Area; leave `IsPersistent` units; linger tile effects per §5
- Win condition: detect last-alive player OR timer expiry at 3600s; fire `IMatchResultSubsystem`

### A5 — NetworkPlayerState

- Inject `ICardLoadingManagerSubsystem` to expand champion `grants_cards` at `SetupDeck()` (§3 Granted Cards)
- `SetupDeck(DeckDetailData, hp, index, name)` — shuffles granted cards directly into the 20-card deck
- `DrawCards(int)` respects max hand size = 6; excess cards go to discard
- Deck-empty check: reshuffle discard immediately before drawing (§13)
- Fire `IHandSubsystem` model update on every hand mutation

### A6 — NetworkUnit

- Inject `IBoardSubsystem` (no `FindFirstObjectByType`)
- Damage pipeline: `TakeDamage(amount, attacker)` calls named `AggregatePass → InterceptPass → CommitPass` (§8)
  - `InterceptPass`: tile effects evaluated first, then unit status effects (§8 §10)
  - `barkskin_ward` reduces by 15, consumed on use
- `MoveToTile(p, q, ignorePathfinding)`: if `ignorePathfinding=true` skip intermediate tile checks — destination only must be empty (§5)
- `ModifyMaxHP(delta)` always mirrors delta to current HP (§11)
- `StartTurn()`: tick cooldowns −1 (§12), resolve tile effects, check evolution (§7)
- `Die()`: subtract `death_anchor` from owning player HP, check elimination (§5 continuous), fire `IBoardSubsystem.UnitDied`

### A7 — NetworkTileEffect

- Registers with `IBoardSubsystem` on `Spawned()`
- One per tile: applying a different lingering effect replaces the existing one (§10)
- Applying same lingering effect refreshes/stacks per individual behavior (§10)
- Owning player's units are immune to their own lingering effect negatives (§10, §2)
- `TickTurn()` decrements `RemainingDuration`; despawn at 0

### A8 — GenericSkillBehaviorSO Refactor

- `Execute(NetworkGameplayManager mgr, IBoardSubsystem board, NetworkUnit caster, HexTile target)` — add `board` parameter
- Replace all `FindObjectsByType<NetworkUnit>()` with `board.GetAllUnits()`
- Replace all `FindFirstObjectByType<BoardManager>()` with the passed `board` reference
- All 23 skill case handlers remain unchanged in behavior

### A9 — FusionSubsystem (new)

- `StageTroop(troopId)`: validates card is in hand, reads innate `grants_skill` → occupies slot 0 if present (§4)
- `StageEquip(slot, equipId)`: validates EquipSpell card in hand + slot ≤4; duplicates allowed (§4)
- `DeployUnit()`: RPC via `FusionNetworkView` → spawn `NetworkUnit` at player's Deploy Area tile; discard all staged cards (§4)
- `FusionNetworkView`: `[Rpc(InputAuthority→StateAuthority)]` validates Deploy Area is empty before spawn

### A10 — DrawPhaseSubsystem (new)

- `NewCards` buffer: 2 cards drawn from deck at DrawPhase start
- Player drags between Keep (max 6 total) and Discard zones
- `ConfirmDraw()`: cards in Discard zone → discard pile; update hand (§5 Draw Phase)
- Auto-confirm on 30s timer expiry: keep first N cards that fit in 6-card limit

### A11 — MatchResultSubsystem (new)

- Subscribe to `IGameStateSubsystem.PhaseChanged` → `GameOver`
- Determine winner per §5 Win Condition (last HP>0, or highest HP on timeout, or Loss on tie)
- Call `IBackendBridgeSubsystem.ReportMatchResultAsync(MatchResultData)`
- Fire `MatchResultReceived` event for UI

---

## 7. Track B — UI Panels

**Owner:** Team Member B
**Scope:** All panel MonoBehaviours, HUD, `PhaseVisibilityController`, `LocalInteractionController`
**Test path:** Lobby → Gameplay (after Track A subsystem interfaces are defined). Build against interface contracts; stubs acceptable during parallel development. Final verification requires Track A complete.

### B1 — PhaseVisibilityController (rewrite)

- Inject `IGameStateSubsystem`
- Subscribe to `PhaseChanged` event (remove `Update()` poll)
- Map `GameplayPhase` enum → `UIIdentifier` → panel activate/deactivate
- Phase → visible panels:
  - `StartPhase`: `GAMEPLAY_SELECTION`
  - `MainPhase`: `GAMEPLAY_FUSION`, `GAMEPLAY_HAND`, `GAMEPLAY_MAIN`
  - `CombatPhase`: `GAMEPLAY_SKILL`, `GAMEPLAY_TURN_ORDER`, `GAMEPLAY_MAIN`
  - `DrawPhase`: `GAMEPLAY_DRAW`, `GAMEPLAY_MAIN`
  - `GameOver`: `GAMEPLAY_MATCH_RESULT`

### B2 — GameplayHUDPanel (rewrite, `GAMEPLAY_MAIN`)

- Inject `IGameStateSubsystem`, `IProfileSubsystem`
- 2× `PlayerHPBar` sub-components (local + opponent) — initialized via `IGameStateSubsystem.GetPlayerState(index)`
- Subscribe to `PhaseChanged`, `RoundChanged`, `TimerChanged` events — no `Update()` polling
- Phase label + round counter + countdown timer display

### B3 — Start Phase Panel (complete, `GAMEPLAY_SELECTION`)

- Complete `GameplayDeckChoosePanel` prefab wiring: `_currentDeckButton`, `_deckSelectOverlay`, `_timerText`, `_confirmButton`
- Hide panel when `IGameplayDeckChooseSubsystem.IsReadyChanged(true)` fires
- Timer display reads from `IGameStateSubsystem.TimerChanged`

### B4 — FusionPanel (new, `GAMEPLAY_FUSION`)

- Inject `IFusionSubsystem`, `IHandSubsystem`, `ICardLoadingManagerSubsystem`
- Hand section: show all cards; click Troop → `IFusionSubsystem.StageTroop(id)`
- 4-slot grid: click EquipSpell card from hand → assign to next free slot → `IFusionSubsystem.StageEquip(slot, id)`
- Slot 0 shown greyed-out if staged troop has innate `grants_skill`
- Duplicate EquipSpell assignments allowed (§4)
- Deploy button (enabled when troop is staged) → `IFusionSubsystem.DeployUnit()`

### B5 — HandPanel (new, `GAMEPLAY_HAND`)

- Inject `IHandSubsystem`, `ICardLoadingManagerSubsystem`
- Subscribe to `IHandSubsystem.HandChanged`
- Display card buttons (max 6); MainPhaseSpell cards show "Play" affordance
- "Play" → `ICombatSubsystem.RequestSkillUse()` for MainPhaseSpell dispatch (thin path via CombatSubsystem)

### B6 — SkillPanel (new, `GAMEPLAY_SKILL`)

- Inject `ICombatSubsystem`, `ICardLoadingManagerSubsystem`
- Visible only when `ICombatSubsystem.TurnStarted` fires for local player's unit
- Up to 4 skill buttons from active unit's skill ID list
- Greyed-out when cooldown > 0 or `one_time` used (§12, §15)
- Click skill → enters targeting mode in `LocalInteractionController`

### B7 — LocalInteractionController (rewrite)

- Inject `IBoardSubsystem`, `ICombatSubsystem`
- Activated by `SkillPanel` when a skill is selected (no self-managed selection)
- Mouse raycast → `HexTile`; highlight range (yellow), valid AOE (green), invalid (red)
- Uses `SkillBehaviorSO.IsTileValidTarget()` to determine color
- On valid tile click → `ICombatSubsystem.RequestSkillUse(skillIndex, targetP, targetQ)`
- Cancel skill → clear highlights

### B8 — TurnOrderPanel (new, `GAMEPLAY_TURN_ORDER`)

- Inject `ICombatSubsystem`
- Subscribe to `TurnStarted` event
- Display speed-sorted queue as list: unit name + HP + owner indicator
- Highlight active unit row

### B9 — DrawPhasePanel (new, `GAMEPLAY_DRAW`)

- Inject `IDrawPhaseSubsystem`, `IHandSubsystem`, `ICardLoadingManagerSubsystem`
- Subscribe to `IDrawPhaseSubsystem.NewCardsChanged`
- Two zones: "Keep" (populated from current hand) and "New Cards" (2 drawn cards)
- Drag cards between zones; Keep zone shows count / max 6
- Confirm button → `IDrawPhaseSubsystem.ConfirmDraw()`
- Timer display from `IGameStateSubsystem`

### B10 — MatchResultPanel (new, `GAMEPLAY_MATCH_RESULT`)

- Inject `IMatchResultSubsystem`, `ISceneLoaderSubsystem`
- Subscribe to `MatchResultReceived`
- Display: winner name, XP gained, Gold gained, win/loss label
- "Return to Lobby" → `ISceneLoaderSubsystem.LoadScene(Constants.LOBBY)`

---

## 8. GameplayInstaller Final Binding List

File: `Features/Gameplay/DI/GameplayInstaller.cs`

```csharp
// StartPhase
Container.BindInterfacesAndSelfTo<GameplayDeckSubsystem>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<GameplayDeckChooseModel>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<GameplayDeckChooseController>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<GameplayDeckChooseSubsystem>().AsSingle().NonLazy();
// GameState
Container.BindInterfacesAndSelfTo<GameStateModel>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<GameStateController>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<GameStateSubsystem>().AsSingle().NonLazy();
// Board
Container.BindInterfacesAndSelfTo<BoardModel>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<BoardController>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<BoardSubsystem>().AsSingle().NonLazy();
// Combat
Container.BindInterfacesAndSelfTo<CombatModel>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<CombatController>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<CombatSubsystem>().AsSingle().NonLazy();
// FusePhase
Container.BindInterfacesAndSelfTo<FusionModel>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<FusionController>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<FusionSubsystem>().AsSingle().NonLazy();
// DrawPhase
Container.BindInterfacesAndSelfTo<DrawPhaseModel>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<DrawPhaseController>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<DrawPhaseSubsystem>().AsSingle().NonLazy();
// Hand
Container.BindInterfacesAndSelfTo<HandModel>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<HandSubsystem>().AsSingle().NonLazy();
// GameOver
Container.BindInterfacesAndSelfTo<MatchResultModel>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<MatchResultController>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<MatchResultSubsystem>().AsSingle().NonLazy();
```

---

## 9. UIIdentifier Usage Map

From `Constants.UIIdentifier` (already defined):

| Panel | Identifier | Phase |
|---|---|---|
| `GameplayHUDPanel` | `GAMEPLAY_MAIN` | All except GameOver |
| `GameplayDeckChoosePanel` | `GAMEPLAY_SELECTION` | StartPhase |
| `FusionPanel` | `GAMEPLAY_FUSION` | MainPhase |
| `HandPanel` | `GAMEPLAY_HAND` | MainPhase |
| `SkillPanel` | `GAMEPLAY_SKILL` | CombatPhase |
| `TurnOrderPanel` | `GAMEPLAY_TURN_ORDER` | CombatPhase |
| `DrawPhasePanel` | `GAMEPLAY_DRAW` | DrawPhase |
| `MatchResultPanel` | `GAMEPLAY_MATCH_RESULT` | GameOver |

---

## 10. Verification

### Track A — Solo Test (no UI required)
1. Lobby → Play vs AI → Gameplay as host, `FillRoomWithAI = true`, `PlayerCnt = 2`
2. Console shows phase transitions: `Setup → StartPhase → MainPhase → CombatPhase → DrawPhase → ...`
3. `IGameStateSubsystem.PhaseChanged` event fires for each transition (test via debug subscriber)
4. `IBoardSubsystem.FindTile()` returns valid tiles; no `FindObjectsByType` calls in logs
5. AI unit completes full turn: move + attack + end turn
6. Damage pipeline: 3 passes execute; `barkskin_ward` absorbs 15 correctly
7. Deck-empty → discard shuffles → draw proceeds without error
8. Board clear: Persistent Units survive; Deploy Area clears
9. Player HP reaches 0 → `MatchResultSubsystem.MatchResultReceived` fires with correct winner
10. No `NullReferenceException` in console

### Track B — Full Flow Test
1. Lobby → Gameplay; `GAMEPLAY_SELECTION` panel visible immediately
2. StartPhase: select deck → confirm → panel hides → game advances to MainPhase
3. MainPhase: `FusionPanel` shows hand cards; stage Troop → 4 slots display; add EquipSpell → Deploy creates unit on Deploy Area
4. CombatPhase: `SkillPanel` shows local unit's 4 skills with cooldown indicators when it's player's turn; click skill → tile highlights appear; click valid tile → attack resolves
5. `TurnOrderPanel` shows queue; active unit highlighted
6. DrawPhase: `DrawPhasePanel` shows 2 new cards + hand; can move to discard; confirm advances round
7. GameOver: `MatchResultPanel` shows winner + XP/Gold; "Return to Lobby" loads Lobby scene
