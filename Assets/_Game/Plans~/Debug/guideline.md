# F1 Architecture Tour & Debugging Guideline

## The Cast of Characters (F1 scope)

| Feature | What it does | Key file |
|---|---|---|
| F1.1 Bootstrap | Spawns everything else | `Features/Gameplay/Scripts/GameState/GameplayNetworkCoordinator.cs` |
| F1.2 Board | Generates 61 hex tiles | `Features/Gameplay/Scripts/Board/BoardNetworkView.cs` |
| F1.3 Phase machine | Timer, phase transitions | `Features/Gameplay/Scripts/GameState/GameStateNetworkView.cs` |
| F1.4 HUD shell | Phase label, match clock | `Features/Gameplay/Scripts/UI/GameplayHUDController.cs` |
| F1.5 Profile | Player name + HP slots | `Features/Gameplay/Scripts/UI/GameplayPlayerProfileUI.cs` |

---

## The Three-Layer Pattern (GameState as the reference)

Every domain is split across exactly three C# classes and one set of interfaces. GameState is the confirmed-working example.

### Layer 1: Model — dumb data store

`Features/Gameplay/Scripts/GameState/GameStateModel.cs`

```
_phase, _phaseTimeRemaining, _matchElapsed, _roundNumber, _currentCombatActor
   ↑
   All written by one method: ApplyState(GameStateData data)
   No logic. No network. Just Observable<T> fields.
```

The `Observable<T>` fires `OnChanged` whenever `.Value` changes. That is all this class does.

### Layer 2: Controller — decision maker

`Features/Gameplay/Scripts/GameState/GameStateController.cs`

```
Holds:  IGameStateModel _model
        IGameStateNetworkBridge _bridge  ← nullable, set by NetworkView after Spawned()

Does:   RegisterBridge()                        ← called by Subsystem when NetworkView spawns
        OnAuthoritativeStateReceived(data)       ← calls _model.ApplyState(), nothing else
```

GameState's controller is thin because players cannot request phase changes. For subsystems that have client-driven intents (Combat, DeckChoose), the controller stages input here and picks local-vs-networked path.

### Layer 3: Subsystem — the public face

`Features/Gameplay/Scripts/GameState/GameStateSubsystem.cs`

```
Initialize():  subscribes to every Observable in the model
               calls _controller.Initialize()

Events:        PhaseChanged, PhaseTimeRemainingChanged, MatchElapsedChanged,
               RoundNumberChanged, CurrentCombatActorChanged

Properties:    Phase, PhaseTimeRemaining, MatchElapsed, RoundNumber, CurrentCombatActor
               ↑ pass-throughs to _model.XxxField.Value
```

The subsystem is the **only** class `[Inject]`ed outside this stack. The HUD, the NetworkView, any other subsystem — all get `IGameStateSubsystem`. Model and controller are `internal` and invisible.

Every event invoke is wrapped in `try/catch` — exceptions from subscribers never propagate back.

---

## The Seam: NetworkView bridges Fusion ↔ Zenject

`Features/Gameplay/Scripts/GameState/GameStateNetworkView.cs`

### Spawned() — runs on every client when the object is network-instantiated

```
1. DI fallback: if GameObjectContext injection didn't fire, pull from SceneContext
   if (_gameState == null) { _gameState = ctx.Container.Resolve<IGameStateSubsystem>(); }

2. Register as bridge
   _gameState.RegisterNetworkBridge(this);

3. StateAuthority only: write initial networked state
   CurrentPhase = StartPhase;
   PhaseTimer   = TickTimer.CreateFromSeconds(Runner, 30f);

4. Push current state to subsystem (handles both host and late-joining client)
   PushState();
```

### FixedUpdateNetwork() — runs every simulation tick, StateAuthority only

```
if (!Object.HasStateAuthority) return;

MatchElapsed += Runner.DeltaTime;
if (PhaseTimer.Expired(Runner))     HandlePhaseTimeout();
if (Phase == StartPhase && AllReady) TransitionTo(MainPhase);
```

### Render() → PushState() — runs on ALL clients every render frame

```
Render():   DetectChanges on [Networked] props → PushState() on any change

PushState():  builds GameStateData from Networked props
              → subsystem.OnAuthoritativeStateReceived(data)
              → controller.OnAuthoritativeStateReceived(data)
              → model.ApplyState(data)
              → Observable fires
              → Subsystem events fire
              → UI updates
```

---

## F1.1 Bootstrap: Who Spawns What

`GameplayNetworkCoordinator` is a NetworkBehaviour. StateAuthority runs `Spawned()`:

```
GameplayNetworkCoordinator.Spawned() [StateAuthority only]
├── SpawnGameStateManager()       → Runner.Spawn(_gameStateManagerPrefab)
├── SpawnBoard()                  → Runner.Spawn(_boardManagerPrefab)
└── SpawnExistingPlayers()        → for each player in Runner.ActivePlayers:
    ├── Runner.Spawn(_playerStatePrefab,       inputAuthority: player)
    ├── Runner.Spawn(_playerCardZoneViewPrefab, inputAuthority: player)
    ├── Runner.Spawn(_deckChooseViewPrefab,     inputAuthority: player)
    └── Runner.Spawn(piecePrefab, at BoardView.GetDeployWorldPosition(index))
```

`_networkManager.PlayerJoined` is also hooked so latecomers trigger the same spawn sequence.

---

## F1.2 Board: How 61 Tiles Are Generated

`BoardNetworkView.GenerateBoard()` runs on StateAuthority only:

```
for r in [-4, 4]:
  numCols = 9 - |r|         → row sizes: 5,6,7,8,9,8,7,6,5 = 61 tiles total
  for c in [0, numCols):
    x = (c - (numCols-1)/2) * horizontalSpacing    (default 1.732f)
    z = r * verticalSpacing                          (default 1.5f)
    p = -r
    q = c - 4 + max(0, r)
    Runner.Spawn(_hexTilePrefab, spawnPos, Euler(270, 330, 0))
    hexTile.SetCoordinates(p, q)

IsGenerated = true
RegisterWithSubsystem()   → boardSubsystem.RegisterTiles(coords, positions)
RegisterDeployAreas()     → player 0 → (4,-4),  player 1 → (-4,4)
```

Clients that join after host do not re-generate. They call `RebuildTileRegistryFromChildren()` which reads already-spawned child `HexTile` objects and rebuilds the local position dictionary.

`BoardSubsystem` holds the resulting dictionaries. All subsystems that need world positions call `IBoardSubsystem.GetWorldPosition(coord)`.

---

## F1.4/F1.5 HUD + Profile

`GameplayHUDController` (on root layout prefab):

```
OnEnable():
  _gameState.PhaseChanged      → update _phaseNameText
  _gameState.MatchElapsedChanged → update _matchTimeText
  Deactivate _enemy2ProfileRoot
  TryBindProfiles()  — retries in Update() until runner has 2 active players
```

`GameplayPlayerProfileUI` (on each Profile_Gameplay prefab instance):

```
Bind(playerRef, isLocal)   ← called by HUDController once two players are known
  → stores _playerRef, calls Refresh() for immediate display

OnEnable():
  _cardZone.HPChanged    → filter by _playerRef → _hpText
  _cardZone.NameChanged  → filter by _playerRef → _nameText
  _deckChoose.IsReadyChanged → if isLocal → _readyToggle
```

`PanelVisibilityRouter` (drives which phase panel is active):

```
_phasePanels[] = [ {StartPhase, Panel_A}, {MainPhase, Panel_B}, ... ]
PhaseChanged → disable all panels → enable the one matching current phase
```

---

## Debugging Pipeline

When something breaks, answer these five questions in order.

---

### Q1: Did Zenject wire up correctly?

**Symptom:** `NullReferenceException` inside a subsystem, or an `[Inject]` field is null at runtime.

**Check:** Open the Gameplay scene's `SceneContext` GameObject. `GameplayInstaller` must be listed as an installer. If a binding is missing from `Features/Gameplay/DI/GameplayInstaller.cs`, the entire downstream stack for that subsystem is null.

**Console signal:** `ZenjectException: Unable to resolve type` — search for this.

---

### Q2: Did the NetworkView spawn and register its bridge?

**Symptom:** Subsystem events never fire. Networked state changes happen on host but UI never updates.

**Log prefixes to check:** `[GameState]`, `[Board]`, `[GameplayNetworkCoordinator]`

`GameStateController.RegisterBridge()` logs `Bridge registered` when the NetworkView calls through. If you never see that log:

1. Is the prefab assigned to the correct field on `GameplayNetworkCoordinator`?
2. Does console show `[GameplayNetworkCoordinator] Spawned GameStateManager`?
3. Does console show `[GameState] Bridge registered`?

- (1) yes, (2) no → `SpawnGameStateManager()` failed (prefab invalid or not StateAuthority).
- (2) yes, (3) no → NetworkView spawned but injection failed inside `Spawned()`.

---

### Q3: Is StateAuthority on the right client?

**Symptom:** Phase machine never ticks; board never generates; all clients stuck.

`FixedUpdateNetwork()` and `GenerateBoard()` both guard `if (!Object.HasStateAuthority) return`. If no client has authority over the object, nothing runs.

**Check:** In Play mode, select the `GameStateManager` NetworkObject in the hierarchy. Fusion's component inspector shows `State Authority: [PlayerRef]`. Should be the host (PlayerRef 1).

---

### Q4: Is Render() / PushState() firing?

**Symptom:** StateAuthority has the correct state but clients don't see it.

`ChangeDetector` only fires when a `[Networked]` property changes value. Writing the same value twice produces no event.

**Quick test:** Add a temporary `Debug.Log` inside `PushState()`. If it fires on host but not on client, the client's `_changeDetector` is likely null — meaning `Spawned()` returned early (injection failure at step 1 of `Spawned()`).

---

### Q5: Is the event subscriber alive?

**Symptom:** Observable fires, PushState runs, but a specific UI element does not update.

The `try/catch` in every subsystem event handler swallows subscriber exceptions intentionally. A broken panel won't surface its error unless you look for it.

**Check:** The catch calls `Debug.LogException(ex)` — it appears in the console as a normal red error. Search for the panel's class name in the stack trace.

Also check: did the MonoBehaviour subscribe in `OnEnable` but its `[Inject]` field was null? `GameplayHUDController` and `GameplayPlayerProfileUI` are MonoBehaviours on scene prefabs — they get DI via `SceneContext`, which requires the prefab to exist in the scene's injection hierarchy at load time.

---

## The Full Signal Chain for One Phase Transition

What happens when the host's StartPhase timer expires:

```
[FixedUpdateNetwork — StateAuthority]
  PhaseTimer.Expired(Runner) == true
  HandlePhaseTimeout()
    TransitionTo(GameplayPhase.MainPhase)
      CurrentPhase = MainPhase          ← [Networked] property written

[Render — all clients, next render frame]
  _changeDetector.DetectChanges(this) → CurrentPhase changed
    PushState()
      _gameState.OnAuthoritativeStateReceived(new GameStateData { Phase = MainPhase, ... })
        GameStateController.OnAuthoritativeStateReceived()
          GameStateModel.ApplyState()
            _phase.Value = MainPhase
              Observable.OnChanged fires

[Same render frame, same thread]
  GameStateSubsystem.HandlePhaseChanged()
    PhaseChanged?.Invoke(GameplayPhase.MainPhase)
      GameplayHUDController.OnPhaseChanged()
        _phaseNameText.text = "MAIN PHASE"
      PanelVisibilityRouter.OnPhaseChanged()
        disables StartPhase panel, enables MainPhase panel
```

If any link is missing, the problem is in exactly one of the five layers. Use Q1–Q5 above to isolate it.
