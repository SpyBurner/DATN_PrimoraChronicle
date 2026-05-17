# Networked Subsystem Architecture Guideline

A practical reference for constructing subsystems that operate across both a Zenject DI
container and a Photon Fusion networked environment, while preserving clean MVC separation
and allowing subsystems to call each other through their facade interfaces.

---

## Core Principle: Two Worlds, One Seam

Your architecture has two distinct runtime worlds with incompatible ownership models:

| World | Owner | Lifecycle |
|---|---|---|
| Services, Models, Controllers, Subsystems | Zenject | Installer → `Initialize()` → `Dispose()` |
| NetworkBehaviours, NetworkObjects | Fusion | `Runner.SpawnAsync()` → `Spawned()` → `OnDestroy()` |

The goal is not to unify these worlds — it is to define **one explicit seam** where they
touch, keep that seam thin, and ensure everything on either side stays ignorant of the other.

---

## Layer Responsibilities

```
┌─────────────────────────────────────────────────────┐
│                     UI Layer                        │
│  UIPanel / MonoBehaviour                            │
│  - Captures input, forwards to subsystem            │
│  - Subscribes to subsystem events for display       │
│  - Knows nothing about model, controller, network   │
└────────────────────────┬────────────────────────────┘
                         │ IXxxSubsystem
┌────────────────────────▼────────────────────────────┐
│                  Subsystem Layer                    │
│  XxxSubsystem : IXxxSubsystem                       │
│  - Public facade only                               │
│  - Subscribes to model observables                  │
│  - Translates observable changes to events          │
│  - Delegates intent and bridge registration         │
│    to controller — network view never bypasses it   │◄──── IXxxSubsystem
└────────────────────────┬────────────────────────────┘         │
                         │ IXxxController (internal)            │
┌────────────────────────▼────────────────────────────┐         │
│                 Controller Layer                    │         │
│  XxxController : IXxxController, IXxxStateReceiver  │         │
│  - Holds nullable IXxxNetworkBridge                 │         │
│  - Local path: mutates model directly               │         │
│  - Networked path: delegates to bridge RPC          │         │
│  - IXxxStateReceiver: sole writer to model          │         │
└──────────┬──────────────────────┬───────────────────┘         │
           │ IXxxModel            │ IXxxNetworkBridge            │
┌──────────▼──────────┐  ┌───────▼───────────────────┐         │
│    Model Layer      │  │      Network Seam          │─────────┘
│  XxxModel           │  │  XxxNetworkView            │ injects IXxxSubsystem only
│  - Observables only │  │  : NetworkBehaviour        │ controller is never exposed
│  - No logic         │  │  , IXxxNetworkBridge       │
│  - No network       │  │  - [Networked] properties  │
└─────────────────────┘  │  - RPCs (upstream)         │
                         │  - Render() (downstream)   │
                         │  - Calls subsystem facade  │
                         └───────────────────────────┘
```

---

## Interfaces

Define three public interface boundaries plus one internal controller interface.
The network view only ever injects `IXxxSubsystem` — controller and state receiver
are fully hidden behind it.

```csharp
// 1. Public subsystem facade — what the UI, other subsystems, AND the network view call
public interface IXxxSubsystem : ISubsystem
{
    event UnityAction<string> SomeValueChanged;

    // Intent (upstream)
    void DoSomething(string input);
    Task SubmitAsync();

    // Network registration — called by XxxNetworkView after Spawned()
    void RegisterNetworkBridge(IXxxNetworkBridge bridge);

    // Authoritative sync — called by XxxNetworkView from Render()
    void OnAuthoritativeStateReceived(XxxStateData data);
}

// 2. Controller — internal only, never injected outside the subsystem stack
internal interface IXxxController : IController
{
    void DoSomething(string input);
    Task SubmitAsync();
    void RegisterBridge(IXxxNetworkBridge bridge);
    void OnAuthoritativeStateReceived(XxxStateData data);
}

// 3. Network bridge — what the controller sends commands through
public interface IXxxNetworkBridge
{
    void SendSubmitRpc(string input);
}
```

`IXxxStateReceiver` no longer exists as a standalone public interface. Its responsibility
is absorbed into `IXxxSubsystem`, keeping the controller completely invisible to the
Fusion world. The network view's single injection point is the same facade the UI panel uses.

---

## State Data Struct

Always encapsulate all synced fields in one struct. This prevents positional argument
errors, makes sync points single-line, and means adding a new field only touches one place.

```csharp
public struct XxxStateData
{
    public string SomeValue;
    public bool IsProcessing;
    // add fields here only
}
```

---

## Model

Pure data. Observables only. No logic, no network, no awareness of anything.

```csharp
internal class XxxModel : IXxxModel
{
    private Observable<string> _someValue = new(string.Empty);
    private Observable<bool> _isProcessing = new(false);

    public Observable<string> SomeValue => _someValue;
    public Observable<bool> IsProcessing => _isProcessing;

    public void Initialize() { }

    public void Dispose()
    {
        _someValue.Value = string.Empty;
        _isProcessing.Value = false;
    }

    public void ApplyState(XxxStateData data)
    {
        _someValue.Value = data.SomeValue;
        _isProcessing.Value = data.IsProcessing;
    }
}
```

`ApplyState` is the **only** method that writes to observables. There are no individual
setters exposed. This enforces that all writes come through one path.

---

## Controller

Fully internal. Holds the nullable bridge. Knows about the model. Never exposed outside
the subsystem stack — the network view cannot reach it directly.

```csharp
internal class XxxController : IXxxController
{
    private readonly IXxxModel _model;
    private readonly IDebugLogger _logger;
    private IXxxNetworkBridge _bridge;

    // Staged input — local only, never written to model until server confirms
    private string _stagedInput;

    public XxxController(IXxxModel model, IDebugLogger logger)
    {
        _model = model;
        _logger = logger;
    }

    public void RegisterBridge(IXxxNetworkBridge bridge)
    {
        _bridge = bridge;
        _logger.Log($"[XxxController] Bridge {(bridge == null ? "unregistered" : "registered")}.");
    }

    // ── Intent (UI/subsystem facing) ────────────────────────────────────

    public void DoSomething(string input)
    {
        _stagedInput = input; // stage locally, do not touch model
    }

    public async Task SubmitAsync()
    {
        if (string.IsNullOrEmpty(_stagedInput))
        {
            _logger.LogWarning("[XxxController] Submit called with empty input.");
            return;
        }

        if (_bridge != null)
        {
            // Networked path: fire RPC, wait for server to confirm via Render()
            _bridge.SendSubmitRpc(_stagedInput);
        }
        else
        {
            // Local path: apply directly as if we were the authority
            OnAuthoritativeStateReceived(new XxxStateData { SomeValue = _stagedInput });
        }
    }

    // ── Authoritative sync (subsystem facing, routed from network view) ─

    public void OnAuthoritativeStateReceived(XxxStateData data)
    {
        // Single write path into the model
        _model.ApplyState(data);
    }
}
```

**Why stage input instead of writing to model immediately:**
If you write to the model on input and the server also writes on confirm, the same client
gets two updates. Staging keeps the model clean until the authoritative result arrives.

---

## Subsystem

The single public facade for the entire stack. Subscribes to model observables and
re-emits them as events. Delegates all intent, bridge registration, and authoritative
sync to the controller. The network view injects only this — controller is never exposed.

```csharp
public class XxxSubsystem : IXxxSubsystem
{
    [Inject] private readonly IXxxController _controller;
    [Inject] private readonly IXxxModel _model;

    public event UnityAction<string> SomeValueChanged;

    public void Initialize()
    {
        _model.SomeValue.OnChanged += HandleSomeValueChanged;
    }

    public void Dispose()
    {
        _model.SomeValue.OnChanged -= HandleSomeValueChanged;
    }

    // ── Intent (UI facing) ───────────────────────────────────────────────

    public void DoSomething(string input) => _controller.DoSomething(input);
    public Task SubmitAsync() => _controller.SubmitAsync();

    // ── Network registration (network view facing) ───────────────────────

    public void RegisterNetworkBridge(IXxxNetworkBridge bridge)
        => _controller.RegisterBridge(bridge);

    // ── Authoritative sync (network view facing) ─────────────────────────

    public void OnAuthoritativeStateReceived(XxxStateData data)
        => _controller.OnAuthoritativeStateReceived(data);

    // ── Observable handlers ──────────────────────────────────────────────

    private void HandleSomeValueChanged()
    {
        try { SomeValueChanged?.Invoke(_model.SomeValue.Value); }
        catch (Exception ex) { /* log, never rethrow from observable handler */ }
    }
}
```

---

## Network View (The Seam)

A `NetworkBehaviour` that lives on a Fusion-spawned prefab. It is the only object that
knows about both worlds. Its single Zenject injection is `IXxxSubsystem` — the same
interface the UI panel uses. Controller and state receiver are never visible to it.

```csharp
public class XxxNetworkView : NetworkBehaviour, IXxxNetworkBridge
{
    // Single injection — the public facade only.
    // GameObjectContext on the prefab resolves this on ALL clients,
    // including those where Fusion replicates the prefab automatically.
    [Inject] private readonly IXxxSubsystem _subsystem;

    private ChangeDetector _changeDetector;

    [Networked] public NetworkString<_32> NetworkedSomeValue { get; set; }
    [Networked] public NetworkBool NetworkedIsProcessing { get; set; }

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (HasInputAuthority)
            _subsystem.RegisterNetworkBridge(this); // routed through facade

        PushState(); // sync late joiners and initial state
    }

    public override void OnDestroy()
    {
        if (HasInputAuthority)
            _subsystem.RegisterNetworkBridge(null); // clean up nullable seam
    }

    // ── IXxxNetworkBridge (upstream: client → server) ───────────────────

    public void SendSubmitRpc(string input) => Rpc_RequestSubmit(input);

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestSubmit(string input)
    {
        // SERVER ONLY: validate and write authoritative networked state
        NetworkedSomeValue = input;
        NetworkedIsProcessing = false;
    }

    // ── Downstream: server → all clients ────────────────────────────────

    public override void Render()
    {
        if (_changeDetector == null) return;
        foreach (var _ in _changeDetector.DetectChanges(this))
        {
            PushState();
            break;
        }
    }

    private void PushState()
    {
        // Routed through subsystem facade — controller never touched directly
        _subsystem.OnAuthoritativeStateReceived(new XxxStateData
        {
            SomeValue = NetworkedSomeValue.ToString(),
            IsProcessing = NetworkedIsProcessing
        });
    }
}
```

### Setting Up GameObjectContext on the Prefab

1. Add a `GameObjectContext` component to the network prefab root
2. Add a `MonoInstaller` to the prefab — typically empty, since `IXxxSubsystem` is already
   bound in the SceneContext and inherits automatically:

```csharp
public class XxxNetworkViewInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // IXxxSubsystem resolves from the parent SceneContext automatically.
        // No additional bindings needed unless the prefab has prefab-local dependencies.
    }
}
```

The `GameObjectContext` on the prefab guarantees that Zenject runs its injection on
**every client** where Fusion instantiates the prefab — including remote clients that never
called `Runner.SpawnAsync()` themselves. Because the view only injects `IXxxSubsystem`,
there is no risk of accidentally exposing controller or model internals through the prefab
context.

---

## Network Spawn Coordinator

One central coordinator handles all networked view spawning. Not one spawner per
subsystem. Triggered by game flow events, not by subsystems themselves.

```csharp
public class NetworkSpawnCoordinator : IInitializable, IDisposable
{
    [Inject] private readonly NetworkRunner _runner;
    [Inject] private readonly IGameFlowSubsystem _gameFlow;

    [Inject] private readonly NetworkViewRegistry _registry; // ScriptableObject with prefab refs

    public void Initialize()
    {
        _gameFlow.CombatStarted += OnCombatStarted;
    }

    public void Dispose()
    {
        _gameFlow.CombatStarted -= OnCombatStarted;
    }

    private async void OnCombatStarted()
    {
        // Spawn the networked view — GameObjectContext on the prefab handles injection
        await _runner.SpawnAsync(_registry.CombatNetworkView, Vector3.zero, Quaternion.identity);
    }
}
```

Bind `NetworkRunner` once in your scene installer:

```csharp
public class FusionInstaller : MonoInstaller
{
    [SerializeField] private NetworkRunner _runner;

    public override void InstallBindings()
    {
        Container.BindInstance(_runner).AsSingle();
        Container.Bind<NetworkSpawnCoordinator>().AsSingle().NonLazy();
    }
}
```

---

## Subsystem Calling Another Subsystem

Subsystems communicate through their facade interfaces only. Never through models or
controllers directly. Bind at the scene or project level depending on lifetime.

```csharp
// CombatSubsystem needs to notify UIManager when combat ends
public class CombatSubsystem : ICombatSubsystem
{
    [Inject] private readonly ICombatController _controller;
    [Inject] private readonly ICombatModel _model;
    [Inject] private readonly IUIManagerSubsystem _uiManager; // injected facade only

    private void HandleCombatEnded()
    {
        _uiManager.Show<ResultsPanel>(); // subsystem calls subsystem via interface
    }
}
```

Never inject `CombatModel` into `UIManagerSubsystem` or vice versa. The facade is the
contract. If two subsystems need to share state, that state belongs in a third shared
model bound at the appropriate scope.

---

## Installer Structure

```
ProjectContext (persistent, cross-scene)
└── Bind: IDebugLogger, IHttpServiceSubsystem, IUIManagerSubsystem, NetworkRunner

SceneContext (per-scene)
├── Bind: IXxxModel → XxxModel
├── Bind: IXxxController → XxxController (internal, AsSingle)
├── Bind: IXxxSubsystem → XxxSubsystem (public, AsSingle)
└── Bind: NetworkSpawnCoordinator (NonLazy so it subscribes on Initialize)

NetworkViewPrefab (GameObjectContext, per spawned object)
└── Inherits from SceneContext — IXxxSubsystem resolves automatically
    Controller and model are never visible here
```

Because the controller is now fully internal, its binding does not need to be exposed
as multiple interfaces. A single `AsSingle` binding is sufficient:

```csharp
// In your SceneInstaller
Container.Bind<IXxxModel>().To<XxxModel>().AsSingle();
Container.Bind<IXxxController>().To<XxxController>().AsSingle();
Container.Bind<IXxxSubsystem>().To<XxxSubsystem>().AsSingle();
```

`IXxxStateReceiver` no longer needs a binding — its responsibility is handled internally
by `XxxController.OnAuthoritativeStateReceived`, routed through the subsystem facade.

---

## Data Flow Summary

```
Local input (no network)
Panel → Subsystem → Controller → Model.ApplyState() → Observable → Subsystem event → Panel

Networked input (Fusion)
Panel → Subsystem → Controller → NetworkView.RPC → [StateAuthority writes Networked props]
                                                           ↓
                                                    Render() on ALL clients
                                                           ↓
                                              NetworkView.PushState()
                                                           ↓
                                   Subsystem.OnAuthoritativeStateReceived()  ← facade only
                                                           ↓
                                         Controller.OnAuthoritativeStateReceived()
                                                           ↓
                                              Model.ApplyState()
                                                           ↓
                                         Observable fires on all clients
                                                           ↓
                                         Subsystem event → Panel updates
```

The network view touches the subsystem facade twice — once upstream via
`RegisterNetworkBridge`, once downstream via `OnAuthoritativeStateReceived`. The
controller is never in its call stack directly. The panel and subsystem cannot tell
whether state arrived from a local method call or a Fusion `Render()`.

---

## Checklist: Adding a New Networked Subsystem

- [ ] Define `IXxxSubsystem` with intent methods, `RegisterNetworkBridge`, and `OnAuthoritativeStateReceived`
- [ ] Define `IXxxController` (internal), `IXxxNetworkBridge` (public)
- [ ] Define `XxxStateData` struct with all synced fields
- [ ] Implement `XxxModel` — observables and `ApplyState()` only
- [ ] Implement `XxxController` — staged input, nullable bridge, single `ApplyState` call
- [ ] Implement `XxxSubsystem` — facade, observable subscriptions, event forwarding, bridge and sync delegation
- [ ] Implement `XxxNetworkView` — injects `IXxxSubsystem` only, `[Networked]` props, RPCs, `Render()` → `PushState()`
- [ ] Add `GameObjectContext` + `MonoInstaller` to network prefab (typically empty)
- [ ] Add prefab reference to `NetworkViewRegistry`
- [ ] Add spawn trigger to `NetworkSpawnCoordinator`
- [ ] Bind `IXxxModel`, `IXxxController`, `IXxxSubsystem` as `AsSingle` in scene installer
- [ ] Bind `NetworkRunner` in scene or project installer (once, not per subsystem)
