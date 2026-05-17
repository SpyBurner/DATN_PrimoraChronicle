# NetworkManager ↔ ServerSession — Harmony Plan

**Goal:** `NetworkManager` stays a pure infrastructure subsystem that owns the runner
and connection state on all builds. `ServerSession` is a server-only orchestration
subsystem that sits on top of it. Both run on the headless server; only `NetworkManager`
runs on the client.

**Scope:** Modifications to existing `NetworkManager` files + full `ServerSession`
wiring as defined in `server_subsystems_implementation_plan.md`.

---

## Responsibility Boundary (reference for all decisions below)

```
NetworkManager                          ServerSession
──────────────────────────────────      ──────────────────────────────────
owns: NetworkRunner lifecycle           owns: match lifecycle
owns: connection state observables      owns: BE reporting
owns: INetworkRunnerCallbacks           owns: session start commands
owns: player connected/disconnected     owns: disconnect → end match logic

answers: "is the network up?"           answers: "what should this server do?"
answers: "who is connected?"            answers: "is a match currently running?"

runs on: client + server                runs on: server only (batchMode guard)
knows about: Fusion                     knows about: NetworkManager (via facade)
knows about: nothing above itself       knows about: BackendBridge (via facade)
```

---

## Part 1 — NetworkManager Modifications

These are the only changes needed. `NetworkManager` does not gain any awareness of
`ServerSession`. Information flows out of it via events; it never calls inward.

---

### Step 1.1 — Fix `ProvideInput` in `NetworkManagerController`

**File:** `NetworkManagerController.cs`

`ProvideInput = true` is hardcoded. A dedicated server must never provide input —
it has no local player.

**Change:** In `StartSession()`, replace:
```csharp
Runner.ProvideInput = true;
```
with:
```csharp
Runner.ProvideInput = !Application.isBatchMode;
```

No other changes to `StartSession()`.

---

### Step 1.2 — Add `LastJoinedPlayer` and `LastLeftPlayer` to `INetworkManagerModel`

**File:** `INetworkManagerModel.cs`

Add two new observable properties and their write methods:

```csharp
Observable<PlayerRef> LastJoinedPlayer { get; }
Observable<PlayerRef> LastLeftPlayer   { get; }

void SetLastJoinedPlayer(PlayerRef player);
void SetLastLeftPlayer(PlayerRef player);
```

These carry the specific `PlayerRef` that triggered each callback — distinct from
`PlayerCount` which is a running total. Both are needed: count for UI display,
specific ref for server-side spawn/despawn decisions.

---

### Step 1.3 — Implement the new fields in `NetworkManagerModel`

**File:** `NetworkManagerModel.cs`

Add backing fields with default `PlayerRef.None`:

```csharp
private Observable<PlayerRef> _lastJoinedPlayer = new(PlayerRef.None);
private Observable<PlayerRef> _lastLeftPlayer   = new(PlayerRef.None);

public Observable<PlayerRef> LastJoinedPlayer => _lastJoinedPlayer;
public Observable<PlayerRef> LastLeftPlayer   => _lastLeftPlayer;

public void SetLastJoinedPlayer(PlayerRef player) => _lastJoinedPlayer.Value = player;
public void SetLastLeftPlayer(PlayerRef player)   => _lastLeftPlayer.Value   = player;
```

Reset both to `PlayerRef.None` in `Dispose()`.

> **Note:** These observables fire on every join/leave event, including repeated
> calls with the same `PlayerRef` if Fusion emits duplicate callbacks. Add a
> guard in the setter if needed: only assign and fire if the value actually changed.

---

### Step 1.4 — Populate the new fields in `NetworkManagerController` callbacks

**File:** `NetworkManagerController.cs`

Extend the two existing callbacks. Do not remove the `PlayerCount` update — the
client still needs it for lobby UI:

```csharp
public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
{
    _model.SetPlayerCount(runner.SessionInfo.PlayerCount);
    _model.SetLastJoinedPlayer(player);                    // add
}

public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
{
    _model.SetPlayerCount(runner.SessionInfo.PlayerCount);
    _model.SetLastLeftPlayer(player);                      // add
}
```

`NetworkManagerController` does **not** inject `IServerSessionSubsystem`. It does
not know `ServerSession` exists. The events flow out through the model observables
and the subsystem fires them — `ServerSession` subscribes from the other side.

---

### Step 1.5 — Expose `PlayerJoined` and `PlayerLeft` events on `INetworkManagerSubsystem`

**File:** `INetworkManagerSubsystem.cs`

Add two events:

```csharp
event UnityAction<PlayerRef> PlayerJoined;
event UnityAction<PlayerRef> PlayerLeft;
```

These are universal — clients need `PlayerJoined` too (opponent spawning, HUD
updates). The events fire on all builds. Only `ServerSession`'s reaction to them
is server-only.

---

### Step 1.6 — Wire the new events in `NetworkManagerSubsystem`

**File:** `NetworkManagerSubsystem.cs`

Declare the events:

```csharp
public event UnityAction<PlayerRef> PlayerJoined;
public event UnityAction<PlayerRef> PlayerLeft;
```

In `Initialize()`, subscribe to the new model observables:

```csharp
_model.LastJoinedPlayer.OnChanged += HandleLastJoinedPlayerChanged;
_model.LastLeftPlayer.OnChanged   += HandleLastLeftPlayerChanged;
```

In `Dispose()`, unsubscribe both.

Add handlers:

```csharp
private void HandleLastJoinedPlayerChanged()
    => PlayerJoined?.Invoke(_model.LastJoinedPlayer.Value);

private void HandleLastLeftPlayerChanged()
    => PlayerLeft?.Invoke(_model.LastLeftPlayer.Value);
```

---

### Summary of `NetworkManager` changes

| File | Change |
|---|---|
| `NetworkManagerController.cs` | `ProvideInput` fix; populate `LastJoinedPlayer` / `LastLeftPlayer` in callbacks |
| `INetworkManagerModel.cs` | Add `LastJoinedPlayer`, `LastLeftPlayer` observable properties and setters |
| `NetworkManagerModel.cs` | Implement new fields, reset in `Dispose()` |
| `INetworkManagerSubsystem.cs` | Add `PlayerJoined`, `PlayerLeft` events |
| `NetworkManagerSubsystem.cs` | Subscribe to new model observables, fire new events, unsubscribe in `Dispose()` |

**No other files in the `NetworkManager` subsystem are touched.**

---

## Part 2 — ServerSession Wiring (delta from `server_subsystems_implementation_plan.md`)

The previous plan had `ServerSessionController` injecting a raw `NetworkRunner`
directly and calling `_runner.StartGame()` itself. Now that `NetworkManager` owns
the runner cleanly, `ServerSession` delegates through the facade instead.

These steps **replace** Phase 4.6 and Phase 5 of the previous plan.
All other phases (1–3, 4.1–4.5, 4.7, 6–8) remain unchanged.

---

### Step 2.1 — Remove `NetworkRunner` injection from `ServerSessionController`

**File:** `ServerSessionController.cs` (to be created)

The previous plan injected `NetworkRunner _runner` directly into
`ServerSessionController`. **Do not do this.**

Instead, inject `INetworkManagerSubsystem`:

```csharp
internal class ServerSessionController : IServerSessionController
{
    private readonly IServerSessionModel _model;
    private readonly IBackendBridgeSubsystem _backendBridge;
    private readonly INetworkManagerSubsystem _networkManager;  // ← facade, not runner
    private readonly IDebugLogger _logger;
    ...
}
```

All runner interactions go through `_networkManager`. The controller never touches
`NetworkRunner` directly.

---

### Step 2.2 — `StartSession` delegates to `INetworkManagerSubsystem`

**File:** `ServerSessionController.cs`

```csharp
public async void StartSession(StartSessionCommand cmd)
{
    if (!Application.isBatchMode) return;

    _logger.Log($"[ServerSession] Starting session: {cmd.SessionName}");

    var args = new StartGameArgs
    {
        GameMode    = GameMode.Server,
        SessionName = cmd.SessionName,
        PlayerCount = 2,
        IsVisible   = false,
        IsOpen      = true,
    };

    var success = await _networkManager.StartSession(args);  // delegates via facade

    if (success)
    {
        _model.ApplyState(new ServerSessionStateData
        {
            ActiveSessionName = cmd.SessionName,
            IsRunning         = true,
        });
        _logger.Log($"[ServerSession] Session ready: {cmd.SessionName}");
    }
    else
    {
        _logger.LogError($"[ServerSession] Failed to start session: {_networkManager.ErrorMessage}");
        // Do not mutate model on failure — IsRunning stays false
    }
}
```

---

### Step 2.3 — `OnMatchEnded` delegates shutdown to `INetworkManagerSubsystem`

**File:** `ServerSessionController.cs`

```csharp
public async void OnMatchEnded(MatchResultData result)
{
    if (!Application.isBatchMode) return;
    if (!_networkManager.Runner.IsServer) return;

    _logger.Log($"[ServerSession] Match ended. Reporting to BE...");

    await _backendBridge.ReportMatchResultAsync(result);

    await _networkManager.ShutdownRunner();  // delegates via facade

    _model.ApplyState(new ServerSessionStateData { IsRunning = false });
}
```

> `_networkManager.Runner.IsServer` is the only place `ServerSessionController`
> reaches through the facade to the runner — and only to read a bool, not to call
> any runner method. This is acceptable. If you want to be stricter, add
> `bool IsServer { get; }` to `INetworkManagerSubsystem` and read that instead.

---

### Step 2.4 — `OnPlayerJoined` and `OnPlayerLeft` use the received `PlayerRef` directly

**File:** `ServerSessionController.cs`

These methods receive `PlayerRef` as a parameter (forwarded from the subsystem event).
No runner access needed:

```csharp
public void OnPlayerJoined(PlayerRef player)
{
    if (!Application.isBatchMode) return;

    _model.ApplyState(new ServerSessionStateData
    {
        ActiveSessionName = _model.ActiveSessionName.Value,
        IsRunning         = _model.IsRunning.Value,
        LastJoinedPlayer  = player,
        LastLeftPlayer    = _model.LastLeftPlayer.Value,
    });
}

public void OnPlayerLeft(PlayerRef player)
{
    if (!Application.isBatchMode) return;

    _model.ApplyState(new ServerSessionStateData
    {
        ActiveSessionName = _model.ActiveSessionName.Value,
        IsRunning         = _model.IsRunning.Value,
        LastJoinedPlayer  = _model.LastJoinedPlayer.Value,
        LastLeftPlayer    = player,
    });

    // If no players remain, treat as disconnect-triggered end
    if (_networkManager.PlayerCount == 0)
    {
        OnMatchEnded(new MatchResultData
        {
            SessionName = _model.ActiveSessionName.Value,
            EndReason   = "Disconnect",
        });
    }
}
```

---

### Step 2.5 — Controller owns all external subscriptions in `Initialize` / `Dispose`

**File:** `IServerSessionController.cs`

No interface change needed — `Initialize()` and `Dispose()` are already declared.

**File:** `ServerSessionController.cs`

The controller subscribes to both `IBackendBridgeSubsystem` and
`INetworkManagerSubsystem` in its own `Initialize()`, and tears them down in
`Dispose()`. The subsystem never touches these:

```csharp
public void Initialize()
{
    if (!Application.isBatchMode) return;

    _backendBridge.StartSessionReceived += StartSession;
    _networkManager.PlayerJoined        += OnPlayerJoined;
    _networkManager.PlayerLeft          += OnPlayerLeft;

    _logger.Log("[ServerSession] Controller initialized.");
}

public void Dispose()
{
    _backendBridge.StartSessionReceived -= StartSession;
    _networkManager.PlayerJoined        -= OnPlayerJoined;
    _networkManager.PlayerLeft          -= OnPlayerLeft;
}
```

---

### Step 2.6 — `ServerSessionSubsystem` is clean — no cross-SS injection

**File:** `ServerSessionSubsystem.cs`

The subsystem injects only its own stack. It has no knowledge of `NetworkManager`
or `BackendBridge`. Its sole responsibilities are: translate own model observables
into public events, and delegate intent methods to the controller.

All external subscriptions (`BackendBridge`, `NetworkManager`) are wired inside
`ServerSessionController.Initialize()` — see Step 2.2 below.

```csharp
public class ServerSessionSubsystem : IServerSessionSubsystem
{
    private readonly IServerSessionController _controller;
    private readonly IServerSessionModel      _model;
    ...
}
```

`Initialize()`:

```csharp
public void Initialize()
{
    // Own model observables → public events only
    _model.LastJoinedPlayer.OnChanged += () =>
        PlayerJoined?.Invoke(_model.LastJoinedPlayer.Value);
    _model.LastLeftPlayer.OnChanged += () =>
        PlayerLeft?.Invoke(_model.LastLeftPlayer.Value);
    _model.IsRunning.OnChanged += () =>
    {
        if (!_model.IsRunning.Value) MatchEnded?.Invoke();
    };

    _controller.Initialize(); // controller wires all external subscriptions
}
```

`Dispose()`:

```csharp
public void Dispose()
{
    _model.LastJoinedPlayer.OnChanged -= ...;
    _model.LastLeftPlayer.OnChanged   -= ...;
    _model.IsRunning.OnChanged        -= ...;

    _controller.Dispose(); // controller tears down all external subscriptions
}
```

---

### Step 2.7 — Update the installer to remove `NetworkRunner` direct binding for `ServerSession`

**File:** `ServerSubsystemsInstaller.cs`

The previous plan's Phase 6.1 did not bind `NetworkRunner` directly — that binding
lives in `FusionInstaller` / `CoreInstaller` for `NetworkManager`'s use. Confirm
that `INetworkManagerSubsystem` is resolvable from `ProjectContext` (it should be,
since `NetworkManager` is already a `ProjectContext` subsystem). No new bindings
needed for this change.

The `ServerSubsystemsInstaller` bindings remain exactly as written in Phase 6.1
of the previous plan.

---

## Part 3 — Final Dependency Graph

```
ProjectContext
│
├── NetworkManagerSubsystem          (client + server)
│       │
│       ├── owns: NetworkRunner lifecycle
│       ├── owns: INetworkRunnerCallbacks
│       ├── fires: PlayerJoined, PlayerLeft (all builds)
│       └── fires: RunnerStateChanged, ErrorMessageChanged
│
├── BackendBridgeSubsystem           (server only — batchMode guard)
│       │
│       ├── owns: HttpListener (inbound from BE)
│       ├── fires: StartSessionReceived, ForceEndMatchReceived
│       └── delegates outbound to: IHttpServiceSubsystem
│
└── ServerSessionSubsystem           (server only — batchMode guard)
        │
        ├── owns: match lifecycle, BE reporting, disconnect handling
        ├── translates: own model observables → public events only
        │
        └── ServerSessionController (internal)
                ├── injects: INetworkManagerSubsystem  ← subscribes PlayerJoined/Left
                │                                         calls StartSession/ShutdownRunner
                └── injects: IBackendBridgeSubsystem   ← subscribes StartSessionReceived
                                                          calls ReportMatchResultAsync
```

---

## Part 4 — Verification Additions

Add these checks to the Phase 7 checklist from the previous plan:

### NetworkManager
- [ ] `PlayerJoined` event fires on client build when a second player joins (lobby test)
- [ ] `PlayerJoined` event fires on headless build when a client connects
- [ ] `ProvideInput` is `false` in headless build (confirm via log or Fusion inspector)
- [ ] `LastJoinedPlayer` observable resets to `PlayerRef.None` after `ShutdownRunner()`

### ServerSession ↔ NetworkManager integration
- [ ] `ServerSessionController.StartSession()` calls `_networkManager.StartSession()`,
  not `runner.StartGame()` directly
- [ ] `ServerSessionController.OnMatchEnded()` calls `_networkManager.ShutdownRunner()`,
  not `runner.Shutdown()` directly
- [ ] `NetworkManagerController` has zero references to `IServerSessionSubsystem`
- [ ] `NetworkManagerSubsystem` has zero references to `IServerSessionSubsystem`
- [ ] Dependency direction is one-way: `ServerSession → NetworkManager`, never reversed
- [ ] `ServerSessionSubsystem` has zero injected fields other than `IServerSessionController` and `IServerSessionModel`
- [ ] `ServerSessionSubsystem.Initialize()` contains zero references to `IBackendBridgeSubsystem` or `INetworkManagerSubsystem`
- [ ] All cross-subsystem subscriptions (`StartSessionReceived`, `PlayerJoined`, `PlayerLeft`) are wired inside `ServerSessionController.Initialize()` only