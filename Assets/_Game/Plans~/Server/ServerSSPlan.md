# Server Subsystems — Implementation Plan

**Subsystems covered:** `BackendBridgeSubsystem`, `ServerSessionSubsystem`  
**Zenject scope:** `ProjectContext` (both)  
**Applies to:** `DATN_PrimoraChronicle`

> **Before starting:** Both subsystems follow the same 7-file structure as every other
> subsystem in the project. Review `networked-subsystem-guideline.md` and
> `subsystem_architecture.md` before writing any code.
>
> Both subsystems live in `ProjectContext`. Their `Initialize()` methods guard on
> `Application.isBatchMode` — they bind and resolve on every build, but do nothing at
> runtime in non-headless builds. Zero cost on clients.

---

## Folder Structure

```
Assets/
└── Core/
    └── Server/
        ├── BackendBridge/
        │   ├── IBackendBridgeModel.cs
        │   ├── IBackendBridgeController.cs        (internal)
        │   ├── IBackendBridgeSubsystem.cs
        │   ├── BackendBridgeModel.cs
        │   ├── BackendBridgeController.cs
        │   └── BackendBridgeSubsystem.cs
        ├── ServerSession/
        │   ├── IServerSessionModel.cs
        │   ├── IServerSessionController.cs        (internal)
        │   ├── IServerSessionSubsystem.cs
        │   ├── ServerSessionModel.cs
        │   ├── ServerSessionController.cs
        │   └── ServerSessionSubsystem.cs
        ├── Data/
        │   ├── StartSessionCommand.cs
        │   ├── MatchResultData.cs
        │   └── BackendBridgeStateData.cs
        └── Installers/
            └── ServerSubsystemsInstaller.cs
```

---

## Phase 1 — Shared Data Structs

Create these first. Both subsystems depend on them.

### Step 1.1 — `StartSessionCommand.cs`

```
Path: Assets/Core/Server/Data/StartSessionCommand.cs
```

Fields:
- `string SessionName` — the Photon session name the headless server should create
- `string Player1UserId`
- `string Player2UserId`
- `int RegionCode` — optional, for future multi-region support

Plain serializable class (no MonoBehaviour, no NetworkBehaviour). Decorated with
`[Serializable]` for JSON deserialization via `JsonConvert`.

### Step 1.2 — `MatchResultData.cs`

```
Path: Assets/Core/Server/Data/MatchResultData.cs
```

Fields:
- `string SessionName`
- `string WinnerUserId`
- `string LoserUserId`
- `int DurationSeconds`
- `string EndReason` — `"Normal"`, `"Disconnect"`, `"Timeout"`

### Step 1.3 — `BackendBridgeStateData.cs`

```
Path: Assets/Core/Server/Data/BackendBridgeStateData.cs
```

This is the model state struct — same pattern as `XxxStateData` in the guideline.

Fields:
- `StartSessionCommand PendingStartSession` — null when idle
- `bool IsListening`

---

## Phase 2 — MainThreadDispatcher

The `HttpListener` runs on a background thread. Unity and Fusion APIs are not
thread-safe. This utility bridges the two.

### Step 2.1 — `MainThreadDispatcher.cs`

```
Path: Assets/Core/Server/MainThreadDispatcher.cs
```

- Plain `MonoBehaviour`, attached to a persistent `GameObject` in `ProjectContext`
- Holds a `ConcurrentQueue<Action>`
- Static `Enqueue(Action)` method — callable from any thread
- `Update()` drains the queue on the Unity main thread each frame

```csharp
// Usage from background thread (inside BackendBridgeController):
MainThreadDispatcher.Enqueue(() => _model.ApplyState(newState));
```

> **Note:** If your project already has a `MainThreadDispatcher`, skip this step and
> use the existing one.

---

## Phase 3 — BackendBridgeSubsystem

This subsystem owns two responsibilities:
1. **Inbound** — runs an `HttpListener` so the BE can POST commands to this server
2. **Outbound** — delegates to `IHttpServiceSubsystem` to POST results back to the BE

### Step 3.1 — `IBackendBridgeSubsystem.cs` (public interface)

```
Path: Assets/Core/Server/BackendBridge/IBackendBridgeSubsystem.cs
```

Extends `ISubsystem`.

Events (inbound commands received from BE):
- `event UnityAction<StartSessionCommand> StartSessionReceived`
- `event UnityAction ForceEndMatchReceived`

Methods (outbound calls to BE, called by `ServerSessionSubsystem`):
- `Task ReportMatchResultAsync(MatchResultData result)`
- `Task ReportPlayerDisconnectedAsync(string userId)`

### Step 3.2 — `IBackendBridgeController.cs` (internal interface)

```
Path: Assets/Core/Server/BackendBridge/IBackendBridgeController.cs
```

Marked `internal`. Extends `IController`.

Mirrors the subsystem's public surface but stays hidden from everything outside the
subsystem stack:
- `void Initialize()`
- `void Dispose()`
- `Task ReportMatchResultAsync(MatchResultData result)`
- `Task ReportPlayerDisconnectedAsync(string userId)`

### Step 3.3 — `IBackendBridgeModel.cs`

```
Path: Assets/Core/Server/BackendBridge/IBackendBridgeModel.cs
```

Marked `internal`. Extends `IModel`.

Properties (read-only observables):
- `Observable<StartSessionCommand> PendingStartSession`
- `Observable<bool> IsListening`

Method:
- `void ApplyState(BackendBridgeStateData data)`

### Step 3.4 — `BackendBridgeModel.cs`

```
Path: Assets/Core/Server/BackendBridge/BackendBridgeModel.cs
```

- Implements `IBackendBridgeModel`
- Private `Observable<StartSessionCommand>` and `Observable<bool>` backing fields
- `ApplyState()` is the **only** write path — sets both observables from the struct
- `Initialize()` sets defaults: `PendingStartSession = null`, `IsListening = false`
- `Dispose()` resets to defaults

### Step 3.5 — `BackendBridgeController.cs`

```
Path: Assets/Core/Server/BackendBridge/BackendBridgeController.cs
```

Implements `IBackendBridgeController`. Constructor-injected dependencies:
- `IBackendBridgeModel _model`
- `IHttpServiceSubsystem _http` — reuses the existing core subsystem for outbound calls
- `IDebugLogger _logger`

`Initialize()`:
- Guard: `if (!Application.isBatchMode) return;`
- Instantiate `HttpListener`, add prefix `http://*:7070/`
- Start listener
- Start background `Thread` running `ListenLoop()`
- Call `_model.ApplyState(new BackendBridgeStateData { IsListening = true })`

`ListenLoop()` (background thread):
- Blocking `_listener.GetContext()` loop
- Each context dispatched to `ThreadPool.QueueUserWorkItem` → `HandleRequest(context)`

`HandleRequest(HttpListenerContext context)`:
- Read `context.Request.Url.AbsolutePath` and `HttpMethod`
- Route:
  - `POST /start-session` → deserialize `StartSessionCommand`, call
    `MainThreadDispatcher.Enqueue(() => _model.ApplyState(new BackendBridgeStateData { PendingStartSession = cmd, IsListening = true }))`, respond 200
  - `POST /force-end-match` → enqueue empty state with a sentinel flag, respond 200
  - Default → respond 404
- Always write JSON response with correct `Content-Type: application/json`

Outbound methods (delegate to `IHttpServiceSubsystem`):
```
ReportMatchResultAsync  → _http.Post("/api/matches/result", result)
ReportPlayerDisconnectedAsync → _http.Post("/api/players/disconnected", payload)
```

`Dispose()`:
- `_listener?.Stop()`
- The background thread is `IsBackground = true` — it terminates automatically with the process

### Step 3.6 — `BackendBridgeSubsystem.cs`

```
Path: Assets/Core/Server/BackendBridge/BackendBridgeSubsystem.cs
```

Implements `IBackendBridgeSubsystem`. Constructor-injected:
- `IBackendBridgeController _controller`
- `IBackendBridgeModel _model`

`Initialize()`:
- Subscribe `_model.PendingStartSession.OnChanged` → `HandlePendingStartSessionChanged`
- Call `_controller.Initialize()`

`HandlePendingStartSessionChanged()`:
- If `_model.PendingStartSession.Value != null`, fire `StartSessionReceived`
- After firing, clear the pending command via `_controller` to avoid re-firing on re-subscribe

`Dispose()`:
- Unsubscribe all observable handlers
- Call `_controller.Dispose()`

Outbound delegation:
- `ReportMatchResultAsync` → `_controller.ReportMatchResultAsync`
- `ReportPlayerDisconnectedAsync` → `_controller.ReportPlayerDisconnectedAsync`

---

## Phase 4 — ServerSessionSubsystem

This subsystem owns the Fusion session lifecycle. It listens to `IBackendBridgeSubsystem`
events and drives `NetworkRunner.StartGame()` / `Shutdown()`.

### Step 4.1 — `IServerSessionSubsystem.cs` (public interface)

```
Path: Assets/Core/Server/ServerSession/IServerSessionSubsystem.cs
```

Extends `ISubsystem`.

Events:
- `event UnityAction<PlayerRef> PlayerJoined`
- `event UnityAction<PlayerRef> PlayerLeft`
- `event UnityAction MatchEnded`

Methods:
- `void OnMatchEnded(MatchResultData result)` — called by game flow when the match
  concludes (e.g. from `GameStateSubsystem`)

### Step 4.2 — `IServerSessionController.cs` (internal interface)

```
Path: Assets/Core/Server/ServerSession/IServerSessionController.cs
```

Marked `internal`. Extends `IController`.

- `void Initialize()`
- `void Dispose()`
- `void StartSession(StartSessionCommand cmd)`
- `void OnPlayerJoined(PlayerRef player)`
- `void OnPlayerLeft(PlayerRef player)`
- `void OnMatchEnded(MatchResultData result)`

### Step 4.3 — `IServerSessionModel.cs`

```
Path: Assets/Core/Server/ServerSession/IServerSessionModel.cs
```

Marked `internal`. Extends `IModel`.

Properties:
- `Observable<string> ActiveSessionName`
- `Observable<bool> IsRunning`
- `Observable<PlayerRef> LastJoinedPlayer`
- `Observable<PlayerRef> LastLeftPlayer`

Method:
- `void ApplyState(ServerSessionStateData data)`

### Step 4.4 — `ServerSessionStateData.cs`

```
Path: Assets/Core/Server/Data/ServerSessionStateData.cs
```

Fields:
- `string ActiveSessionName`
- `bool IsRunning`
- `PlayerRef LastJoinedPlayer`
- `PlayerRef LastLeftPlayer`

### Step 4.5 — `ServerSessionModel.cs`

```
Path: Assets/Core/Server/ServerSession/ServerSessionModel.cs
```

Same pattern as `BackendBridgeModel`. `ApplyState()` is the only write path.

`Initialize()` defaults: empty session name, `IsRunning = false`.

### Step 4.6 — `ServerSessionController.cs`

```
Path: Assets/Core/Server/ServerSession/ServerSessionController.cs
```

Implements `IServerSessionController`. Constructor-injected:
- `IServerSessionModel _model`
- `IBackendBridgeSubsystem _backendBridge` — subsystem calls subsystem via facade only
- `NetworkRunner _runner` — the single bound `NetworkRunner` from `FusionInstaller`
- `IDebugLogger _logger`

`Initialize()`:
- Guard: `if (!Application.isBatchMode) return;`
- Log ready state

`StartSession(StartSessionCommand cmd)`:
- Guard: `if (!Application.isBatchMode) return;`
- Call `await _runner.StartGame(new StartGameArgs { GameMode = GameMode.Server, SessionName = cmd.SessionName, PlayerCount = 2, IsVisible = false, IsOpen = true })`
- On success: `_model.ApplyState(new ServerSessionStateData { ActiveSessionName = cmd.SessionName, IsRunning = true })`
- On failure: log error, do not mutate model

`OnPlayerJoined(PlayerRef player)`:
- Guard: `if (!_runner.IsServer) return;`
- `_model.ApplyState(... LastJoinedPlayer = player ...)`

`OnPlayerLeft(PlayerRef player)`:
- Guard: `if (!_runner.IsServer) return;`
- `_model.ApplyState(... LastLeftPlayer = player ...)`
- If active player count drops to 0, trigger `OnMatchEnded` with `EndReason = "Disconnect"`

`OnMatchEnded(MatchResultData result)`:
- Guard: `if (!_runner.IsServer) return;`
- `await _backendBridge.ReportMatchResultAsync(result)`
- `_runner.Shutdown()`
- `_model.ApplyState(new ServerSessionStateData { IsRunning = false })`

### Step 4.7 — `ServerSessionSubsystem.cs`

```
Path: Assets/Core/Server/ServerSession/ServerSessionSubsystem.cs
```

Implements `IServerSessionSubsystem`. Constructor-injected:
- `IServerSessionController _controller`
- `IServerSessionModel _model`
- `IBackendBridgeSubsystem _backendBridge`

`Initialize()`:
- Subscribe `_backendBridge.StartSessionReceived` → `_controller.StartSession`
- Subscribe `_model.LastJoinedPlayer.OnChanged` → fires `PlayerJoined` event
- Subscribe `_model.LastLeftPlayer.OnChanged` → fires `PlayerLeft` event
- Subscribe `_model.IsRunning.OnChanged` → if false, fire `MatchEnded` event
- Call `_controller.Initialize()`

`Dispose()`:
- Unsubscribe all
- Call `_controller.Dispose()`

`OnMatchEnded(MatchResultData result)`:
- Delegates to `_controller.OnMatchEnded(result)`

---

## Phase 5 — Fusion Callbacks Bridge

`NetworkRunner` fires `INetworkRunnerCallbacks` — these need to reach
`IServerSessionSubsystem`. The cleanest place is your existing `NetworkManager`
subsystem, which already owns the `NetworkRunner`.

### Step 5.1 — Wire callbacks in `NetworkManagerController`

In your existing `NetworkManagerController` (or wherever `INetworkRunnerCallbacks` is
implemented):

```
OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    → if (Application.isBatchMode) _serverSession.OnMatchEnded / PlayerJoined
```

Inject `IServerSessionSubsystem` into `NetworkManagerController`. Call
`_serverSession`'s public method on each relevant callback.

> **Why here?** `NetworkManager` already owns runner callbacks. Putting session
> reporting there keeps `ServerSessionController` free of `INetworkRunnerCallbacks`
> and avoids registering a second callback handler on the runner.

Callbacks to wire:
- `OnPlayerJoined` → `IServerSessionSubsystem` (routed internally to controller)
- `OnPlayerLeft` → `IServerSessionSubsystem`
- `OnShutdown` → guard + log only, session already cleaned up by controller

---

## Phase 6 — Installer

### Step 6.1 — `ServerSubsystemsInstaller.cs`

```
Path: Assets/Core/Server/Installers/ServerSubsystemsInstaller.cs
```

Extends `MonoInstaller`. Attach to the `ProjectContext` prefab alongside `CoreInstaller`.

```csharp
public class ServerSubsystemsInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // BackendBridge
        Container.Bind<IBackendBridgeModel>()
            .To<BackendBridgeModel>().AsSingle();
        Container.Bind<IBackendBridgeController>()
            .To<BackendBridgeController>().AsSingle();
        Container.Bind<IBackendBridgeSubsystem>()
            .To<BackendBridgeSubsystem>().AsSingle().NonLazy();

        // ServerSession
        Container.Bind<IServerSessionModel>()
            .To<ServerSessionModel>().AsSingle();
        Container.Bind<IServerSessionController>()
            .To<ServerSessionController>().AsSingle();
        Container.Bind<IServerSessionSubsystem>()
            .To<ServerSessionSubsystem>().AsSingle().NonLazy();
    }
}
```

Both are `NonLazy` so `Initialize()` fires on startup and the `HttpListener` is ready
before any BE connection attempt.

### Step 6.2 — Update `subsystem_architecture.md`

Add to the Core Subsystems table:

| Subsystem | Description | Dependencies |
|---|---|---|
| **BackendBridge** | Inbound `HttpListener` from BE + outbound reporting. Headless only (batchMode guard). | `HttpService` |
| **ServerSession** | Manages Fusion session lifecycle on the headless server. | `NetworkManager`, `BackendBridge` |

---

## Phase 7 — Verification Checklist

### BackendBridgeSubsystem
- [ ] `Initialize()` does nothing in a normal client build (confirm with log)
- [ ] `Initialize()` starts `HttpListener` on port 7070 in headless build (confirm with log)
- [ ] POST `/start-session` from Postman → `StartSessionReceived` event fires on main thread
- [ ] POST `/start-session` with malformed JSON → returns 400, no crash
- [ ] `ReportMatchResultAsync` calls the correct BE endpoint via `HttpService`
- [ ] `Dispose()` stops the listener cleanly (no port conflict on restart)

### ServerSessionSubsystem
- [ ] `StartSession()` only runs in batchMode build
- [ ] `StartSession()` creates a visible Fusion session (confirm in Photon dashboard)
- [ ] Two clients can join the created session by name
- [ ] `OnPlayerLeft` with 0 remaining players triggers `OnMatchEnded` with `"Disconnect"`
- [ ] `OnMatchEnded` calls `ReportMatchResultAsync` before `runner.Shutdown()`
- [ ] `runner.Shutdown()` fires and session disappears from Photon dashboard

### Integration
- [ ] Full flow: BE POST → session created → clients join → match ends → result POSTed to BE
- [ ] No `IServerSessionSubsystem` or `IBackendBridgeSubsystem` methods execute in client builds
- [ ] No null reference exceptions when `NetworkRunner` is not yet started and callbacks fire

---

## Phase 8 — Testing Without a Headless Build

During early development you can test the flow in **Host Mode** without a full headless
build. Use this temporary override in `ServerSessionController.StartSession()`:

```csharp
var mode = Application.isBatchMode ? GameMode.Server : GameMode.Host;

await _runner.StartGame(new StartGameArgs {
    GameMode    = mode,
    SessionName = cmd.SessionName,
    // ...
});
```

This lets you trigger `StartSession()` manually (e.g. via a dev UI button or editor
script) and verify the full subsystem chain — observable fires, session creates, clients
join — before committing to a headless build pipeline.

Remove the override before shipping.