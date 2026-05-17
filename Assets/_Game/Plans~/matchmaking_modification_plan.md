# MatchMaking + BattleSetup — Modification Plan

**Goal:** Wire `BattleSetupSubsystem` parameters (game mode, player count, fill with AI)
into `MatchMakingSubsystem` so that `MatchMakingController` can construct the correct
`StartGameArgs` and hand off to `NetworkManager`. Covers Host Mode connection test.

**Prerequisite reading:** `server_subsystems_implementation_plan.md`,
`networkmanager_serversession_harmony_plan.md`

---

## Responsibility Boundary

```
BattleSetupSubsystem      stores: PlayMode, PlayerCount, FillWithAI
                          answers: "what kind of match does the player want?"

MatchMakingSubsystem      reads: BattleSetup state (via controller injection)
                          does:  build StartGameArgs, call NetworkManager,
                                 manage searching/found/cancelled status
                          answers: "are we currently finding or in a match?"

NetworkManager            does:  runner.StartGame(), exposes RunnerState
MatchMakingPanel          does:  show status, cancel/accept/reject buttons only
```

---

## Part 1 — BattleSetupSubsystem (existing, verify only)

You said `BattleSetupSubsystem` already exists. Confirm it exposes the following
before starting Part 2. If any are missing, add them.

### Step 1.1 — Confirm `IBattleSetupSubsystem` exposes these readable properties

```csharp
public interface IBattleSetupSubsystem : ISubsystem
{
    PlayMode  SelectedPlayMode  { get; }  // enum: TwoPlayer, ThreeVsOne
    int       PlayerCount       { get; }  // 2 or 3
    bool      FillWithAI        { get; }  // only valid when PlayerCount == 2
}
```

Where `PlayMode` is:

```csharp
public enum PlayMode { TwoPlayer, ThreeVsOne }
```

If the enum doesn't exist yet, add it to `Assets/Core/Server/Data/` or the
existing shared data folder.

### Step 1.2 — Confirm `BattleSetupModel` has matching observables

The model must have:
- `Observable<PlayMode> SelectedPlayMode`
- `Observable<int> PlayerCount`
- `Observable<bool> FillWithAI`

These are read by `MatchMakingController` via the subsystem facade. `BattleSetupController`
is the only writer, per the architecture rule.

---

## Part 2 — MatchMakingModel Changes

### Step 2.1 — Remove individual setters, add `ApplyState`

**File:** `MatchMakingModel.cs`

Current model uses individual setters (`SetStatus`, `SetTimer`, `SetPlayerJoinedCount`).
This is the known architecture violation from `implementation_plan_p1.md`. Fix it now
since we are touching this file.

Add a state struct:

```
Path: Assets/Features/Lobby/Scripts/MatchMaking/MatchMakingStateData.cs
```

```csharp
public struct MatchMakingStateData
{
    public string         Status;
    public float          Timer;
    public int            PlayerJoinedCount;
    public MatchMakingPhase Phase;
}
```

Where:

```csharp
public enum MatchMakingPhase
{
    Idle,
    Searching,
    MatchFound,    // confirmation window open
    Connecting,    // StartGame in progress
    Connected,     // RunnerState == Running
    Cancelled,
    Failed
}
```

Replace `IMatchMakingModel` individual setters with:

```csharp
public interface IMatchMakingModel : IModel
{
    Observable<string>           Status          { get; }
    Observable<float>            Timer           { get; }
    Observable<int>              PlayerJoinedCount { get; }
    Observable<MatchMakingPhase> Phase           { get; }

    void ApplyState(MatchMakingStateData data);
}
```

Update `MatchMakingModel.cs` accordingly — `ApplyState` is the only write path.

### Step 2.2 — Add `Phase` observable wiring in `MatchMakingSubsystem`

**File:** `MatchMakingSubsystem.cs`

Add event and subscription:

```csharp
public event UnityAction<MatchMakingPhase> PhaseChanged;
```

In `Initialize()`:
```csharp
_model.Phase.OnChanged += () => PhaseChanged?.Invoke(_model.Phase.Value);
```

In `Dispose()`, unsubscribe.

Also add to `IMatchMakingSubsystem`:
```csharp
event UnityAction<MatchMakingPhase> PhaseChanged;
MatchMakingPhase CurrentPhase { get; }
```

---

## Part 3 — MatchMakingController Changes

This is the main change. The controller gains `IBattleSetupSubsystem` as an injected
dependency and uses it to build `StartGameArgs`.

### Step 3.1 — Inject `IBattleSetupSubsystem` into `MatchMakingController`

**File:** `MatchMakingController.cs`

Cross-subsystem dependency lives in the controller, per the architecture rule.

```csharp
internal class MatchMakingController : IMatchMakingController
{
    [Inject] private readonly IDebugLogger             _debugLogger;
    [Inject] private readonly IMatchMakingModel        _model;
    [Inject] private readonly INetworkManagerSubsystem _networkManager;
    [Inject] private readonly ISceneLoaderSubsystem    _sceneLoader;
    [Inject] private readonly IBattleSetupSubsystem    _battleSetup;   // ← add
}
```

### Step 3.2 — Replace `StartMatchmaking` with correct `StartGameArgs` construction

**File:** `MatchMakingController.cs`

Remove the hardcoded `GameMode.Shared`. Build args from `BattleSetup` state.

For a **Host Mode connection test**, the host always uses `GameMode.Host`.
The joining client uses `GameMode.Client` + the session name. The distinction
is made via a parameter on `StartMatchmaking`:

Update `IMatchMakingController` and `IMatchMakingSubsystem`:

```csharp
// IMatchMakingSubsystem
Task StartAsHost();
Task StartAsClient(string sessionName);
Task CancelMatchmaking();
Task AcceptMatch();
Task RejectMatch();
```

> **Why two methods instead of one?** In Host Mode, host and client follow different
> code paths. A single `StartMatchmaking()` would need an enum parameter or a flag,
> which makes the intent less clear at the call site. Separate methods also make
> `MatchMakingPanel` button wiring explicit. When you add the BE-driven matchmaking
> later, you add `StartQueuedMatchmaking()` without touching the existing two.

#### `StartAsHost()` implementation:

```csharp
public async Task StartAsHost()
{
    try
    {
        _model.ApplyState(new MatchMakingStateData
        {
            Phase  = MatchMakingPhase.Connecting,
            Status = "Starting session..."
        });

        var args = new StartGameArgs
        {
            GameMode    = GameMode.Host,
            PlayerCount = _battleSetup.PlayerCount,
            SessionName = GenerateSessionName(),   // guid-based, or derived from player id
            IsVisible   = true,
            IsOpen      = true,
        };

        bool success = await _networkManager.StartSession(args);

        if (success)
        {
            _model.ApplyState(new MatchMakingStateData
            {
                Phase  = MatchMakingPhase.Connected,
                Status = "Waiting for opponent..."
            });
        }
        else
        {
            _model.ApplyState(new MatchMakingStateData
            {
                Phase  = MatchMakingPhase.Failed,
                Status = $"Failed: {_networkManager.ErrorMessage}"
            });
        }
    }
    catch (Exception ex)
    {
        _debugLogger.LogError($"[MatchMaking] StartAsHost failed: {ex.Message}");
        _model.ApplyState(new MatchMakingStateData
        {
            Phase  = MatchMakingPhase.Failed,
            Status = $"Error: {ex.Message}"
        });
    }
}

private string GenerateSessionName()
    => $"session_{System.Guid.NewGuid().ToString()[..8]}";
```

#### `StartAsClient()` implementation:

```csharp
public async Task StartAsClient(string sessionName)
{
    try
    {
        _model.ApplyState(new MatchMakingStateData
        {
            Phase  = MatchMakingPhase.Connecting,
            Status = $"Joining session {sessionName}..."
        });

        var args = new StartGameArgs
        {
            GameMode    = GameMode.Client,
            SessionName = sessionName,
        };

        bool success = await _networkManager.StartSession(args);

        if (success)
        {
            _model.ApplyState(new MatchMakingStateData
            {
                Phase  = MatchMakingPhase.Connected,
                Status = "Connected!"
            });
        }
        else
        {
            _model.ApplyState(new MatchMakingStateData
            {
                Phase  = MatchMakingPhase.Failed,
                Status = $"Failed: {_networkManager.ErrorMessage}"
            });
        }
    }
    catch (Exception ex)
    {
        _debugLogger.LogError($"[MatchMaking] StartAsClient failed: {ex.Message}");
        _model.ApplyState(new MatchMakingStateData
        {
            Phase  = MatchMakingPhase.Failed,
            Status = $"Error: {ex.Message}"
        });
    }
}
```

### Step 3.3 — Wire `RunnerStateChanged` → scene load in `Initialize`

**File:** `MatchMakingController.cs`

The controller subscribes to `NetworkManager.RunnerStateChanged` in its own
`Initialize()`. When the runner reaches `Running`, trigger the scene load.
This is the cross-subsystem subscription that belongs in the controller.

Add to `IMatchMakingController` — no change needed, `Initialize`/`Dispose` already
declared.

```csharp
public void Initialize()
{
    _networkManager.RunnerStateChanged  += HandleRunnerStateChanged;
    _networkManager.PlayerCountChanged  += HandlePlayerCountChanged;
}

public void Dispose()
{
    _networkManager.RunnerStateChanged  -= HandleRunnerStateChanged;
    _networkManager.PlayerCountChanged  -= HandlePlayerCountChanged;
}

private void HandleRunnerStateChanged(NetworkRunner.States state)
{
    if (state == NetworkRunner.States.Running)
    {
        _model.ApplyState(new MatchMakingStateData
        {
            Phase  = MatchMakingPhase.Connected,
            Status = "Connected!"
        });
        // Scene load is triggered here — controller owns cross-SS calls
        _sceneLoader.LoadScene(SceneToken.Gameplay);
    }

    if (state == NetworkRunner.States.Shutdown)
    {
        _model.ApplyState(new MatchMakingStateData
        {
            Phase  = MatchMakingPhase.Idle,
            Status = string.Empty
        });
    }
}

private void HandlePlayerCountChanged(int count)
{
    _model.ApplyState(new MatchMakingStateData
    {
        Phase             = _model.Phase.Value,
        Status            = _model.Status.Value,
        Timer             = _model.Timer.Value,
        PlayerJoinedCount = count
    });
}
```

### Step 3.4 — Fix `AcceptMatch` and `RejectMatch`

**File:** `MatchMakingController.cs`

`AcceptMatch` is currently empty. With the corrected flow, the scene load happens
automatically via `HandleRunnerStateChanged` when `RunnerState == Running`. The
confirmation window (`MatchFound` phase) is only needed for **BE-driven queued
matchmaking** (future), not for Host Mode.

For now:

```csharp
public Task AcceptMatch()
{
    // In Host Mode: no-op. Scene load is driven by RunnerStateChanged.
    // Reserved for queued matchmaking confirmation (Phase 2).
    return Task.CompletedTask;
}

public async Task RejectMatch()
{
    await _networkManager.ShutdownRunner();
    _model.ApplyState(new MatchMakingStateData
    {
        Phase  = MatchMakingPhase.Idle,
        Status = string.Empty
    });
}
```

---

## Part 4 — MatchMakingSubsystem Changes

### Step 4.1 — Add `StartAsHost` and `StartAsClient` to subsystem

**File:** `MatchMakingSubsystem.cs`

Remove `StartMatchmaking()`. Add:

```csharp
public Task StartAsHost()         => _controller.StartAsHost();
public Task StartAsClient(string sessionName) => _controller.StartAsClient(sessionName);
```

Keep `CancelMatchmaking`, `AcceptMatch`, `RejectMatch` as-is.

### Step 4.2 — Fix `Dispose` unsubscribe gap

**File:** `MatchMakingSubsystem.cs`

Current `Dispose()` unsubscribes `Timer` but not `Status`. Fix:

```csharp
public void Dispose()
{
    if (_model?.Status != null)
        _model.Status.OnChanged -= HandleStatusChanged;
    if (_model?.Timer != null)
        _model.Timer.OnChanged  -= HandleConfirmationTimerChanged;
    if (_model?.Phase != null)
        _model.Phase.OnChanged  -= HandlePhaseChanged;
}
```

### Step 4.3 — Expose `CurrentPhase` pass-through

```csharp
public MatchMakingPhase CurrentPhase => _model.Phase.Value;
```

---

## Part 5 — MatchMakingPanel Changes

### Step 5.1 — Remove `INetworkManagerController` injection

**File:** `MatchMakingPanel.cs`

```csharp
// REMOVE — panel must not inject network infrastructure directly
[Inject] private readonly INetworkManagerController _networkManager;
```

The panel only knows about `IMatchMakingSubsystem` and `IUIManagerSubsystem`.
All network state is communicated via `MatchMaking` events.

### Step 5.2 — Add Host / Client buttons and session name input

**File:** `MatchMakingPanel.cs`

For the Host Mode connection test, the panel needs:
- **Host button** → `_matchMaking.StartAsHost()`
- **Join button** → `_matchMaking.StartAsClient(_sessionNameInput.text)`
- **Session name input field** (TMP_InputField) — visible only when joining
- Existing cancel/accept/reject buttons remain

```csharp
[SerializeField] private Button           _hostButton;
[SerializeField] private Button           _joinButton;
[SerializeField] private TMP_InputField   _sessionNameInput;
```

Wire in `OnEnable`:
```csharp
_hostButton?.onClick.AddListener(OnHost);
_joinButton?.onClick.AddListener(OnJoin);
```

Unsubscribe in `OnDisable`.

Handlers:
```csharp
private void OnHost() => _matchMaking.StartAsHost();
private void OnJoin() => _matchMaking.StartAsClient(_sessionNameInput.text);
```

### Step 5.3 — Drive all visuals from `PhaseChanged`

**File:** `MatchMakingPanel.cs`

Subscribe to `PhaseChanged` in `OnEnable`, unsubscribe in `OnDisable`.
`UpdateVisuals()` reads `CurrentPhase` from the subsystem:

```csharp
_matchMaking.PhaseChanged += OnPhaseChanged;

private void OnPhaseChanged(MatchMakingPhase phase) => UpdateVisuals(phase);

private void UpdateVisuals(MatchMakingPhase phase)
{
    bool isIdle        = phase == MatchMakingPhase.Idle;
    bool isConnecting  = phase == MatchMakingPhase.Connecting;
    bool isConnected   = phase == MatchMakingPhase.Connected;

    _hostButton.gameObject.SetActive(isIdle);
    _joinButton.gameObject.SetActive(isIdle);
    _sessionNameInput.gameObject.SetActive(isIdle);
    _cancelButton.gameObject.SetActive(isConnecting || isConnected);
    _acceptButton.gameObject.SetActive(false);   // reserved for queued matchmaking
    _rejectButton.gameObject.SetActive(false);   // reserved for queued matchmaking
}
```

---

## Part 6 — NetworkRunner Prefab Setup (Unity Editor)

This is a Unity setup step, not a code step. Required for `StartGame()` to succeed.

### Step 6.1 — Create a `NetworkRunner` prefab

1. Create an empty `GameObject` in a setup scene, name it `[NetworkRunner]`
2. Add component: `NetworkRunner`
3. Add component: `NetworkSceneManagerDefault`
4. Set `NetworkRunner.ProvideInput = false` in Inspector (runtime override handles it)
5. Save as a prefab at `Assets/Core/Network/Prefabs/NetworkRunner.prefab`

### Step 6.2 — Inject prefab into `NetworkManagerController`

Rather than `new GameObject()` + `AddComponent`, instantiate the prefab:

In `NetworkManagerController`, add:
```csharp
[Inject] private readonly NetworkRunner _runnerPrefab;
```

In `StartSession()`, replace the `new GameObject` block:
```csharp
// Before:
var go = new GameObject("[NetworkRunner]");
Runner = go.AddComponent<NetworkRunner>();

// After:
Runner = GameObject.Instantiate(_runnerPrefab);
GameObject.DontDestroyOnLoad(Runner.gameObject);
```

Bind the prefab in your installer:
```csharp
Container.Bind<NetworkRunner>()
    .FromComponentInNewPrefab(networkRunnerPrefab)  // SO or direct reference
    .AsSingle();
```

Or expose it as a `[SerializeField]` on the installer MonoBehaviour and bind via
`Container.BindInstance(_runnerPrefab)`.

---

## Part 7 — Scene Load After Connection

### Step 7.1 — Confirm `SceneToken.Gameplay` exists

`SceneLoader` uses a `SceneToken` or equivalent enum/SO to identify scenes. Confirm
`Gameplay` is registered. If not, add it to the token list.

### Step 7.2 — No additional wiring needed

`HandleRunnerStateChanged` in `MatchMakingController.Initialize()` (Step 3.3) already
calls `_sceneLoader.LoadScene(SceneToken.Gameplay)` when `RunnerState == Running`.
This is the complete trigger chain:

```
runner.StartGame() succeeds
    → Fusion fires runner state internally
    → NetworkManagerController.OnPlayerJoined / session info updates
    → NetworkManagerModel.RunnerState set to Running
    → NetworkManagerSubsystem.RunnerStateChanged fires
    → MatchMakingController.HandleRunnerStateChanged()
    → _sceneLoader.LoadScene(SceneToken.Gameplay)
    → Gameplay scene loads
```

---

## Part 8 — Files Changed Summary

| File | Change |
|---|---|
| `MatchMakingStateData.cs` | **New** — state struct replacing individual setters |
| `MatchMakingPhase.cs` | **New** — enum for matchmaking lifecycle phases |
| `IMatchMakingModel.cs` | Replace setters with `ApplyState`, add `Phase` observable |
| `MatchMakingModel.cs` | Implement `ApplyState`, add `Phase` backing field |
| `IMatchMakingController.cs` | Replace `StartMatchmaking` with `StartAsHost` / `StartAsClient` |
| `MatchMakingController.cs` | Full rewrite of `StartMatchmaking`; add `IBattleSetupSubsystem` inject; wire `RunnerStateChanged` in `Initialize` |
| `IMatchMakingSubsystem.cs` | Replace `StartMatchmaking` with `StartAsHost` / `StartAsClient`; add `PhaseChanged` event |
| `MatchMakingSubsystem.cs` | Add `PhaseChanged` wiring; fix `Dispose` gap; delegate new methods |
| `MatchMakingPanel.cs` | Remove `INetworkManagerController` inject; add Host/Join buttons; drive visuals from `PhaseChanged` |
| `NetworkManagerController.cs` | Replace `new GameObject` runner creation with prefab instantiation |
| `NetworkRunner.prefab` | **New** — Unity prefab with `NetworkSceneManagerDefault` |

---

## Part 9 — Host Mode Connection Test Procedure

Once all changes above are implemented, verify with this manual test:

1. Open two Unity Editor instances (or build one and run in Editor)
2. In Instance 1: navigate to `MatchMakingPanel`, press **Host**
   - Expected: status shows "Waiting for opponent...", session name logged
3. In Instance 2: enter the session name from Instance 1's log, press **Join**
   - Expected: both instances show "Connected!", gameplay scene loads on both
4. Disconnect Instance 2 mid-session
   - Expected: Instance 1 runner state goes to Shutdown, returns to lobby
5. Verify `ProvideInput` is `true` on both instances (Host Mode — both provide input)
   - Contrast with headless: `ProvideInput` would be `false`

---

## Architecture Rule Violations Fixed in This Plan

| File | Violation | Fix |
|---|---|---|
| `MatchMakingController.cs` | `GameMode.Shared` hardcoded | Replaced with args from `BattleSetup` |
| `MatchMakingPanel.cs` | Injects `INetworkManagerController` directly | Removed; panel only knows `IMatchMakingSubsystem` |
| `MatchMakingSubsystem.cs` | `Dispose()` missing `Status` unsubscribe | Fixed |
| `MatchMakingModel.cs` | Individual setters instead of `ApplyState` | Replaced with `ApplyState(struct)` |
| `MatchMakingController.cs` | `AcceptMatch` empty with comment about Fusion scene loading | Clarified intent; scene load moved to `RunnerStateChanged` handler |
