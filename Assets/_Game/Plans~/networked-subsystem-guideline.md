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

## SSoT Under Fusion

In a **local-only** subsystem (Auth, Profile, Deck-listing) the Model is the single source
of truth — it owns all mutable state and all writes go through `ApplyState()`.

When Fusion is involved the SSoT shifts to the wire:

| Subsystem type | Where the SSoT lives | Model's role |
|---|---|---|
| Local-only | `XxxModel` | Owns state |
| Networked — singleton view | `[Networked]` props on the single NetworkObject | Downstream projection |
| Networked — per-player views | N `[Networked]` blocks, one per player's NetworkObject | Downstream projection, aggregated into a `PlayerRef`-keyed dict |

`Model.ApplyState()` is still the only write path into the model, but in the networked case
it is called *after* Fusion: `Render()` → `PushState()` → `subsystem.OnAuthoritativeStateReceived()`
→ `controller.OnAuthoritativeStateReceived()` → `model.ApplyState()`. This path runs on
**every peer including the StateAuthority** — the server does not bypass it.

N NetworkObjects + 1 Model does not mean N sources of truth. There is one model (the dict),
one set of `[Networked]` props per NetworkObject, and the model is the observable cache of
the wire — not its replacement.

---

## Topology

Choose before implementing. The spawn shape determines the bridge registration pattern.

| Topology | When | Examples | Notes |
|---|---|---|---|
| **Singleton** | Match-wide state | `GameStateNetworkView`, `BoardNetworkView`, `CombatNetworkView` | One NetworkObject. `HasStateAuthority` writes only. No per-owner dict. |
| **Per-player, always-replicated** | Public per-player data every client must see | `PlayerRosterPublicNetworkView` | One NO per player, all replicated to all peers. Each view registers itself by `Owner` in `Spawned()` unconditionally. Model holds a `PlayerRef`-keyed dict. |
| **Per-player, AoI-restricted** | Private per-player data only the owner should see | `PlayerCardZonePrivateNetworkView`, `MatchRewardsPrivateNetworkView` | Same per-owner registration. Add `Runner.SetPlayerAlwaysInterested(Owner, Object, true)` on `HasStateAuthority` in `Spawned()` — only the owner's peer ever receives this object. |
| **Per-entity** | One per unit / tile-effect | `UnitPublicNetworkView`, `UnitPrivateNetworkView` | Same per-owner registration pattern; identity key is `NetworkId` + `Owner`. |

The singleton pattern uses a **single nullable bridge** in the controller. The per-player
and per-entity patterns use a **`Dictionary<PlayerRef, IXxxNetworkBridge>`** keyed by owner,
so every replicated view can register without collision.

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
│  - Singleton: holds nullable IXxxNetworkBridge      │         │
│  - Per-player: holds Dictionary<PlayerRef, bridge>  │         │
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
    // Singleton topology:     RegisterNetworkBridge(IXxxNetworkBridge bridge)
    // Per-player topology:    RegisterNetworkBridge(PlayerRef owner, IXxxNetworkBridge bridge)
    void RegisterNetworkBridge(IXxxNetworkBridge bridge);

    // Authoritative sync — called by XxxNetworkView from Render()
    void OnAuthoritativeStateReceived(XxxStateData data);
}

// 2. Controller — internal only, never injected outside the subsystem stack
internal interface IXxxController : IController
{
    void DoSomething(string input);
    Task SubmitAsync();
    // Singleton:   RegisterBridge(IXxxNetworkBridge bridge)
    // Per-player:  RegisterBridge(PlayerRef owner, IXxxNetworkBridge bridge)
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

**Per-player variant** — when the subsystem holds data for multiple players, the model
holds parallel dicts keyed by `PlayerRef.RawEncoded` rather than scalar observables, and
`ApplyState` emits per-owner events. The single-write-path rule still holds.

```csharp
// Per-player model — one dict per replicated field, keyed by PlayerRef.RawEncoded
internal class XxxModel : IXxxModel
{
    public event Action<PlayerRef, int> ValueChanged;

    private readonly Dictionary<int, int> _values = new();
    private readonly List<PlayerRef> _allOwners = new();

    public IReadOnlyList<PlayerRef> AllOwners => _allOwners;
    public int GetValue(PlayerRef p) => _values.TryGetValue(p.RawEncoded, out var v) ? v : 0;

    public void ApplyState(XxxStateData data)   // data carries Owner + Value
    {
        int key = data.Owner.RawEncoded;
        if (!_allOwners.Contains(data.Owner)) _allOwners.Add(data.Owner);

        bool changed = !_values.TryGetValue(key, out int prev) || prev != data.Value;
        _values[key] = data.Value;
        if (changed) ValueChanged?.Invoke(data.Owner, data.Value);
    }

    public void Dispose() { _values.Clear(); _allOwners.Clear(); }
}
```

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

    // Singleton topology — single nullable bridge
    public void RegisterBridge(IXxxNetworkBridge bridge)
    {
        _bridge = bridge;
        _logger.Log($"[XxxController] Bridge {(bridge == null ? "unregistered" : "registered")}.");
    }

    // Per-player topology — replace the above with this dict variant:
    // private readonly Dictionary<PlayerRef, IXxxNetworkBridge> _bridges = new();
    // public void RegisterBridge(PlayerRef owner, IXxxNetworkBridge bridge)
    // {
    //     if (bridge == null) _bridges.Remove(owner);
    //     else _bridges[owner] = bridge;
    // }

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

### Singleton view

One instance for the whole match. No per-owner registration needed.

```csharp
public class XxxNetworkView : NetworkBehaviour, IXxxNetworkBridge
{
    [Inject(Optional = true)] private IXxxSubsystem _subsystem;

    private ChangeDetector _changeDetector;

    [Networked] public NetworkString<_32> NetworkedSomeValue { get; set; }
    [Networked] public NetworkBool NetworkedIsProcessing { get; set; }

    public override void Spawned()
    {
        if (_subsystem == null)
        {
            var ctx = FindObjectOfType<SceneContext>();
            if (ctx != null) _subsystem = ctx.Container.Resolve<IXxxSubsystem>();
            else { Debug.LogError("[XxxNetworkView] SceneContext not found."); return; }
        }

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        _subsystem.RegisterNetworkBridge(this);
        PushState();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
        => _subsystem?.RegisterNetworkBridge(null);

    public void SendSubmitRpc(string input) => Rpc_RequestSubmit(input);

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestSubmit(string input)
    {
        NetworkedSomeValue = input;
        NetworkedIsProcessing = false;
    }

    public override void Render()
    {
        if (_changeDetector == null) return;
        foreach (var _ in _changeDetector.DetectChanges(this)) { PushState(); break; }
    }

    private void PushState()
    {
        if (_subsystem == null) return;
        _subsystem.OnAuthoritativeStateReceived(new XxxStateData
        {
            SomeValue = NetworkedSomeValue.ToString(),
            IsProcessing = NetworkedIsProcessing
        });
    }
}
```

### Per-player view

One instance per player, spawned by the coordinator with `InputAuthority = player`.
All N replicated instances exist on every peer. The controller holds a `Dictionary<PlayerRef, IXxxNetworkBridge>`.

```csharp
public class XxxNetworkView : NetworkBehaviour, IXxxNetworkBridge
{
    [Inject(Optional = true)] private IXxxSubsystem _subsystem;

    [Networked] public PlayerRef Owner { get; set; }
    [Networked] public int SomeValue { get; set; }

    private ChangeDetector _changeDetector;
    // Cache InputAuthority at Spawned() — Object may be gone by Despawned().
    private PlayerRef _cachedInputAuthority;

    public override void Spawned()
    {
        if (_subsystem == null)
        {
            var ctx = FindFirstObjectByType<SceneContext>();
            if (ctx != null) _subsystem = ctx.Container.Resolve<IXxxSubsystem>();
            else { Debug.LogError("[XxxNetworkView] SceneContext not found."); return; }
        }

        _cachedInputAuthority = Object.InputAuthority;
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        // Register unconditionally — the controller's dict disambiguates by owner.
        // Every peer registers every replicated view; only the matching view on the
        // StateAuthority peer can actually write [Networked] props (HasStateAuthority guard).
        _subsystem.RegisterNetworkBridge(_cachedInputAuthority, this);

        // Local-only init: push identity data the server may not have yet.
        if (Object.HasInputAuthority)
        {
            if (Object.HasStateAuthority)
                SomeValue = GetLocalValue();          // host writes directly
            else
                Rpc_PushLocalData(GetLocalValue());   // client sends RPC to server
        }

        PushState();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
        => _subsystem?.RegisterNetworkBridge(_cachedInputAuthority, null);

    public override void Render()
    {
        if (_changeDetector == null) return;
        foreach (var _ in _changeDetector.DetectChanges(this)) { PushState(); break; }
    }

    private void PushState()
    {
        // Guard Owner so the first Spawned() push (before pre-spawn callback sets Owner)
        // doesn't fire with default values.
        if (_subsystem == null || Owner == PlayerRef.None) return;
        _subsystem.OnAuthoritativeStateReceived(new XxxStateData
        {
            Owner = Owner,
            SomeValue = SomeValue
        });
    }

    // ── IXxxNetworkBridge — server-side write path ───────────────────────
    // These are NOT Fusion RPCs. The server calls them directly on the view reference.
    // HasStateAuthority guard makes them no-ops on non-authority peers.

    public void ServerSetValue(PlayerRef owner, int value)
    {
        if (!Object.HasStateAuthority) return;
        SomeValue = value;
    }

    // ── Upstream RPC (client → server) ──────────────────────────────────

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_PushLocalData(int value) => SomeValue = value;
}
```

**Server-side write flow for remote player data** (e.g. damage from combat):

The server does NOT route HP changes through the subsystem bridge dict. It holds direct
`NetworkView` references (from `GameplayNetworkCoordinator._rosterViews[player]`) and calls
server-side write methods directly:

```csharp
// In a server-side controller after damage resolves:
_rosterViews[targetPlayer].ServerSetValue(targetPlayer, newHP);
// Fusion replicates HP; Render() → PushState() → model.ApplyState() on all clients.
```

The bridge dict is used for the **upstream** path only: local player → subsystem →
controller → `_bridges[localPlayer].SendSomeRpc(...)` → server RPC. Remote players'
bridge entries are stored but dormant for upstream intent.

### Injection pattern for Fusion-spawned NetworkViews

Use `[Inject(Optional = true)]` on all injected fields and resolve from `SceneContext` in
`Spawned()` if the field is still null. Do **not** rely on `GameObjectContext` per-prefab
wiring — Fusion can instantiate prefabs on remote clients in ways that bypass the normal
Unity `Awake()` ordering that `GameObjectContext` depends on, causing silent injection
failures with no exception. The `SceneContext` fallback in `Spawned()` is always reliable
because Fusion never calls `Spawned()` before scene initialization is complete.

This means NetworkView prefabs need **no** `GameObjectContext` or `MonoInstaller` component.
One less thing to wire per subsystem.

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

- [ ] Choose topology: **singleton** / **per-player always-replicated** / **per-player AoI-restricted** / **per-entity**
- [ ] Define `IXxxSubsystem` with intent methods, `RegisterNetworkBridge`, and `OnAuthoritativeStateReceived`
  - Per-player: `RegisterNetworkBridge(PlayerRef owner, IXxxNetworkBridge bridge)`
  - Singleton: `RegisterNetworkBridge(IXxxNetworkBridge bridge)`
- [ ] Define `IXxxController` (internal), `IXxxNetworkBridge` (public)
  - Per-player: controller holds `Dictionary<PlayerRef, IXxxNetworkBridge>`
- [ ] Define `XxxStateData` struct with all synced fields
  - Per-player: include `PlayerRef Owner` field in the struct
- [ ] Implement `XxxModel`
  - Per-player: parallel `Dictionary<int, T>` fields keyed by `PlayerRef.RawEncoded`; `ApplyState` emits per-owner events
  - Singleton: observables only
- [ ] Implement `XxxController` — staged input, bridge (nullable or dict), single `ApplyState` call
- [ ] Implement `XxxSubsystem` — facade, observable/event subscriptions, bridge and sync delegation
- [ ] Implement `XxxNetworkView`
  - Singleton: `RegisterNetworkBridge(this)` + `RegisterNetworkBridge(null)` in `Spawned`/`Despawned`
  - Per-player: cache `_cachedInputAuthority = Object.InputAuthority`; call `RegisterNetworkBridge(_cachedInputAuthority, this)` unconditionally; `PushState()` guards `Owner != PlayerRef.None`; `Despawned` calls `RegisterNetworkBridge(_cachedInputAuthority, null)`
  - AoI-restricted: add `Runner.SetPlayerAlwaysInterested(Owner, Object, true)` on `HasStateAuthority` in `Spawned()`
- [ ] Spawn site: pass `Owner` and initial state via `Runner.Spawn` pre-spawn callback
- [ ] Add prefab reference to `NetworkViewRegistry` (or coordinator's serialized field)
- [ ] Add spawn trigger to coordinator (`GameplayNetworkCoordinator.SpawnPlayerState` for per-player)
- [ ] Bind `IXxxModel`, `IXxxController`, `IXxxSubsystem` as `AsSingle().NonLazy()` in scene installer
- [ ] Bind `NetworkRunner` in scene or project installer (once, not per subsystem)
