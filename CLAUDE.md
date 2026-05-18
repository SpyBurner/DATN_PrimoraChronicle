# Primora Chronicle — Claude Code Reference

Unity card-game project. Two players fight on a hex board using Fusion-networked units.
Backend is a REST API server. Client is built with Zenject DI + Photon Fusion + DOTween.

---

## Repository Layout

```
Assets/
├── _Game/
│   ├── Core/Scripts/DI/        CoreInstaller.cs  ← ProjectContext bindings
│   ├── Features/
│   │   ├── Account/            login / register flow
│   │   ├── Lobby/              deck management, matchmaking, shop, profile
│   │   └── Gameplay/           hex board combat scene
│   ├── Plans~/                 design docs + active task plans (NOT compiled)
│   └── Plugins/Demigiant/      DOTween (HOTween v2) — DLL + module .cs files
├── Scenes/                     Login, Lobby, Gameplay, etc.
└── Packages/                   UPM packages (Zenject, Fusion, etc.)
```

---

## Architecture: MVVM + Zenject + Photon Fusion

Every feature follows the same 7-layer stack. Read
`Assets/_Game/Plans~/networked-subsystem-guideline.md` for the full spec.

### Layer Responsibilities

| Layer | Type | Rule |
|---|---|---|
| **Model** | `internal class XxxModel : IXxxModel` | Observables only (`Observable<T>`). `ApplyState(XxxStateData)` is the only write path. No logic, no network. |
| **Controller** | `internal class XxxController : IXxxController` | Holds staged input + nullable `IXxxNetworkBridge`. Local path: calls `_model.ApplyState()`. Networked path: fires RPC through bridge. |
| **Subsystem** | `public class XxxSubsystem : IXxxSubsystem` | Public facade. Wires model observables → `UnityAction` events. Delegates intent + bridge registration to controller. **Only thing panels and NetworkViews ever inject.** |
| **Panel (View)** | `MonoBehaviour` | Captures input → calls subsystem. Subscribes to subsystem events for display. Never touches model or controller directly. |
| **NetworkView (Seam)** | `NetworkBehaviour + IXxxNetworkBridge` | Bridges Fusion ↔ Zenject. Injects `IXxxSubsystem` (may inject multiple subsystems — NetworkViews are the View layer). `Spawned()` registers bridge. `Render()` calls `PushState()` → `subsystem.OnAuthoritativeStateReceived()`. |
| **StateData** | `public struct XxxStateData` | All synced fields in one struct. Only argument to `ApplyState` and `OnAuthoritativeStateReceived`. |

### Data Flow

```
Local input:
  Panel → Subsystem → Controller → Model.ApplyState() → Observable → Subsystem event → Panel

Networked input (Fusion):
  Panel → Subsystem → Controller → NetworkView.RPC()
       [StateAuthority writes Networked props]
              → Render() on ALL clients
              → NetworkView.PushState()
              → Subsystem.OnAuthoritativeStateReceived()
              → Controller.OnAuthoritativeStateReceived()
              → Model.ApplyState()
              → Observable fires → Subsystem event → Panel updates
```

### Interfaces

```csharp
public interface IXxxSubsystem                    // ← panels + NetworkViews inject this
{
    event UnityAction<T> SomeValueChanged;
    void DoSomething(T input);
    Task SubmitAsync();
    void RegisterNetworkBridge(IXxxNetworkBridge bridge);
    void OnAuthoritativeStateReceived(XxxStateData data);
    void Initialize();
    void Dispose();
}

internal interface IXxxController { ... }         // never exposed outside subsystem stack
public interface IXxxNetworkBridge { void SendRpc(...); }
internal interface IXxxModel { Observable<T> Prop { get; }; void ApplyState(...); }
```

---

## DI Binding Pattern

**Always use `BindInterfacesAndSelfTo<T>().AsSingle().NonLazy()`** — same as LobbyInstaller.

```csharp
// In XxxInstaller.cs
Container.BindInterfacesAndSelfTo<XxxModel>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<XxxController>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<XxxSubsystem>().AsSingle().NonLazy();
```

### Installer Hierarchy

| Installer | File | Scope |
|---|---|---|
| `CoreInstaller` | `Core/Scripts/DI/CoreInstaller.cs` | ProjectContext (global) |
| `LobbyInstaller` | `Features/Lobby/DI/LobbyInstaller.cs` | Lobby SceneContext |
| `GameplayInstaller` | `Features/Gameplay/DI/GameplayInstaller.cs` | Gameplay SceneContext (**currently empty — add new bindings here**) |

### Global subsystems (CoreInstaller → ProjectContext)

`IDebugLogger`, `IUIManagerSubsystem`, `ISceneLoaderSubsystem`, `IHttpServiceSubsystem`,
`IAuthSessionSubsystem` / `IAuthSessionModel`, `IAudioManagerSubsystem`,
`INetworkManagerSubsystem`, `IBackendBridgeSubsystem`, `ICardLoadingManagerSubsystem`

---

## Assembly Definitions

| Assembly | Path | Notes |
|---|---|---|
| `Core.Interfaces` | `Core/Scripts/Interfaces/Core.Interfaces.asmdef` | **ALL interfaces + data structs go here.** `autoReferenced: true` — visible to every assembly without explicit GUID. |
| `GameplayFeatures` | `Features/Gameplay/GameplayFeatures.asmdef` | Gameplay implementations only (no interfaces). References Fusion, Zenject, DOTween.Runtime. |
| `LobbyFeatures` | `Features/Lobby/LobbyFeatures.asmdef` | Lobby implementations only. |
| `DOTween.Runtime` | `Plugins/Demigiant/DOTween/DOTween.Runtime.asmdef` | Wraps DOTween.dll + module .cs files. |

### Interfaces folder layout

```
Core/Scripts/Interfaces/
├── Core.Interfaces.asmdef       (autoReferenced: true)
├── Core/
│   ├── AudioManager/            IAudioManagerSubsystem, IModel, ...
│   ├── AuthSession/
│   ├── HttpService/
│   ├── UIManager/
│   └── ...
└── Features/
    ├── Gameplay/
    │   ├── Board/
    │   ├── Combat/
    │   ├── StartPhase/          ← DeckChoose interfaces live here
    │   ├── FusePhase/
    │   ├── GameState/
    │   └── ...
    └── Lobby/
        ├── Deck/                IDeckSubsystem, IDeckModel, IDeckController, DeckSummaryData
        ├── DeckBuild/
        └── ...
```

**Rule**: every `IXxxSubsystem`, `IXxxController`, `IXxxModel`, `IXxxNetworkBridge`,
and `XxxStateData` struct goes in the matching `Core/Scripts/Interfaces/` subfolder.
Implementations (`XxxModel.cs`, `XxxController.cs`, `XxxSubsystem.cs`, `XxxNetworkView.cs`,
`XxxPanel.cs`) stay in the feature assembly.

**DOTween usage**: always use `DOTween.To(getter, setter, target, duration)` (core API)
rather than extension methods like `DOAnchorPos` — the extension methods live in
`DOTweenModuleUI.cs` inside `DOTween.Runtime` and may not be visible across assemblies.
`transform.DOMove` / `transform.DORotate` work fine (they are in `DOTween.dll` itself).

---

## Gameplay Feature Details

### Hex Board

- Axial coordinates `(P, Q)` — third axis `R = -P - Q`
- Hex distance: `(|Δp| + |Δq| + |Δr|) / 2`
- `BoardManager` — tile grid + `FindTile(p,q)` + `ResolveCoordinateToPosition(p,q)`
- `HexTile` — individual tile with color state
- `NetworkSpawner` — spawns board, tiles, players on session start

### Networked Game Objects

All inherit `NetworkBehaviour` (Photon Fusion):

| Class | Role |
|---|---|
| `NetworkGameplayManager` | Singleton. Phase machine, combat queue, win condition |
| `NetworkPlayerState` | Per-player: HP, hand/deck/discard, `SetupDeck()`, `DrawCards()` |
| `NetworkUnit` | Per-unit: HP, Speed, skills, status effects, `MoveToTile()`, `TakeDamage()` |
| `NetworkTileEffect` | Per-tile effect: type, duration, owner |

### Phase Machine (`NetworkGameplayManager`)

```
Setup → StartPhase (30s) → MainPhase (60s) → CombatPhase → DrawPhase → repeat
```

- **StartPhase**: players choose decks. Timer expiry → `AutoConfirmDecks()`.
- **MainPhase**: fuse units + play spells.
- **CombatPhase**: Speed-sorted action queue, one unit turn at a time. `ParanoidMinimaxAI` handles AI units.
- **DrawPhase**: deal 2 cards per player, increment round.

### Combat Pipeline

`TakeDamage` is a 3-pass system:
1. Aggregate raw damage
2. Intercept (tile effects first, then unit status effects — e.g. `barkskin_ward` reduces by 15)
3. Commit → `HP -= final`

### Skill System

`GenericSkillBehaviorSO` (ScriptableObject) implements all skills by `behaviorId` string.
`NetworkGameplayManager.RPC_RequestSkillExecution()` validates range, target, cooldown, one-time flag, then calls `skill.Execute()`.

### AI

`ParanoidMinimaxAI` — host-authoritative. Weighted eval: Player Pressure + Distance + Tile Effects.
Called from `NetworkGameplayManager.StartNextCombatTurn()` when the current unit's owner is AI.

### UI Panels (Gameplay)

Located in `Features/Gameplay/UI/Component/`. Three anchor prefabs, each with a `PanelDrawer`:

| Prefab | Panel inside | Toggle |
|---|---|---|
| `HandPanelAnchor.prefab` | `PhaseInteractionPanel` | `Toggle_Sidebar` |
| `SkillPanelAnchor.prefab` | `PhaseInteractionPanel` | `Toggle_Sidebar` |
| `TurnOrderPanelAnchor.prefab` | `PhaseInteractionPanel` | `Toggle_Sidebar` |

`PanelDrawer` slides `_panel` between `anchoredPosition = Vector2.zero` (closed) and
the `anchoredPosition` of its child `OpenPosition` (open), using DOTween `OutCubic` easing.
`_toggle` (Toggle component) drives open/close via `onValueChanged`.

Editor tool: `Tools/Primora/Add PanelDrawers to Anchors` — wires PanelDrawer + Toggle_Sidebar on all 3 anchors in the active scene.

---

## Active Work: Gameplay Deck-Choose

**Plan file**: `Assets/_Game/Plans~/deck-choose-gameplay-plan.md`

### What is built

Full subsystem stack in `Features/Gameplay/Scripts/DeckChoose/`:

```
GameplayDeckChooseStateData        struct  — { IsReady, SelectedDeckId }
IGameplayDeckChooseModel           interface (internal)
IGameplayDeckChooseController      interface (internal)
IGameplayDeckChooseSubsystem       interface (public)
IGameplayDeckChooseNetworkBridge   interface (public)
GameplayDeckChooseModel            implementation
GameplayDeckChooseController       implementation — fetches DeckDetailData via /api/decks/{id}
GameplayDeckChooseSubsystem        implementation
GameplayDeckChooseNetworkView      NetworkBehaviour — RPCs → NetworkPlayerState.SetupDeck()
GameplayDeckChoosePanel            MonoBehaviour (Features/Gameplay/Scripts/UI/)
                                   reuses IDeckSubsystem + DeckButton from Lobby
```

### What is NOT yet done (TODO)

1. **`GameplayInstaller.cs`** — add bindings:
   ```csharp
   Container.BindInterfacesAndSelfTo<GameplayDeckChooseModel>().AsSingle().NonLazy();
   Container.BindInterfacesAndSelfTo<GameplayDeckChooseController>().AsSingle().NonLazy();
   Container.BindInterfacesAndSelfTo<GameplayDeckChooseSubsystem>().AsSingle().NonLazy();
   ```

2. **Move DeckChoose interfaces to `Core.Interfaces`** — the 5 interface/data files currently in `Features/Gameplay/Scripts/DeckChoose/` must be moved to `Core/Scripts/Interfaces/Features/Gameplay/StartPhase/`:
   `IGameplayDeckChooseSubsystem.cs`, `IGameplayDeckChooseController.cs`,
   `IGameplayDeckChooseModel.cs`, `IGameplayDeckChooseNetworkBridge.cs`,
   `GameplayDeckChooseStateData.cs`

3. **`GameplayFeatures.asmdef`** — verify it references `LobbyFeatures` GUID (needed for `DeckButton`, `DeckSummaryData`, `IDeckSubsystem`).

3. **`PhaseInteractionPanel_DeckChoose.prefab`** — add `GameplayDeckChoosePanel` component, wire `_deckListContainer`, `_deckButtonPrefab`, `_confirmButton`.

4. **`GameplayDeckChooseNetworkView` prefab** — create NetworkObject prefab, add `GameObjectContext` + empty `MonoInstaller`, register in `NetworkViewRegistry`.

5. **Spawn trigger** — in `NetworkGameplayManager.StartMatch()` or `NetworkSpawnCoordinator`, spawn one `GameplayDeckChooseNetworkView` per player when `StartPhase` begins.

6. **`NetworkGameplayManager.AutoConfirmDecks()`** — currently uses hardcoded defaults. Should call `IGameplayDeckChooseSubsystem.AutoConfirmLastDeck()` via the bridge when timer expires.

7. **Phase-aware panel visibility** — show `GameplayDeckChoosePanel` when `CurrentPhase == StartPhase`, hide when `IsReady == true`.

---

## Coding Conventions

- **No comments** unless the WHY is non-obvious.
- **No `using UnityEditor;`** in runtime scripts — wrap in `#if UNITY_EDITOR`.
- `internal` on Model and Controller classes — they must never be injected outside their subsystem stack.
- Subsystem event handlers must never rethrow: `try { Event?.Invoke(...); } catch (Exception ex) { Debug.LogException(ex); }`
- Stage input in controller; never write to model until server confirms (prevents double-update).
- `[Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]` for client→server RPCs.
- After any `create_script` or `script_apply_edits` via MCP, always wait for compilation then check console errors before proceeding.

---

## MCP / Editor Tooling

- Unity Editor must be open and MCP server running (port 8090) for MCP tools to work.
- `Tools/Primora/Add PanelDrawers to Anchors` — wires PanelDrawer to the 3 anchor prefabs in the active scene.
- `Tools/Primora/Apply PanelDrawer Prefabs and Remove from Scene` — applies prefab overrides and removes temporary scene instances.
- DOTween Utility Panel: `Tools/Demigiant/DOTween Utility Panel`.
