# Gameplay Multiplayer Implementation Plan

**Final deliverable path** (after approval): `Assets/_Game/Plans~/gameplay-multiplayer-plan.md`

## Context

The Gameplay scene is the only feature that currently has no MVVM+Fusion stack — the legacy implementation under `Features/Gameplay/Scripts/LEGACY/` was an experimental monolith that violates the project's 7-layer architecture and is being treated as reference-only for hardcoded values. Meanwhile the Core layer is mature (9 subsystems, full DI in CoreInstaller), the Lobby feature stack (Profile, Deck, MatchMaking) is production-ready, and the in-progress `GameplayDeckChoose` subsystem is already 80% built using the canonical pattern. The UI prefabs for every gameplay phase are authored in `Features/Gameplay/UI/Component/`.

The goal is a **standalone multiplayer match** that, after a player picks a deck in Lobby and matchmaking pairs two human players, runs end-to-end inside the Gameplay scene: Start → Main → Combat → Draw → loop, until a winner exists, then returns to Lobby.

**Decisions confirmed up-front (from clarification):**
- **Split axis:** Engine-Authority (Track A) vs Player-UX (Track B), meeting at frozen `IXxxSubsystem` interfaces.
- **AI scope:** No AI in v1. Human-vs-human only. AI deferred.
- **Runner source:** Started in Lobby via `INetworkManagerSubsystem.StartSession()`, carries into Gameplay via `ISceneLoaderSubsystem.LoadNetworkedScene()`. Gameplay scene installer only adds gameplay-specific bindings.

---

## 1. Pre-Existing Inputs

These subsystems are already implemented and DI-bound. Track A and Track B inject only these interfaces (never their concrete types).

### 1.1 Core subsystems (CoreInstaller, ProjectContext)

| Subsystem | Interface | Used by Gameplay for |
|---|---|---|
| `IDebugLogger` | `Helper.DebugLogger` | All logs |
| `IUIManagerSubsystem` | `Core/UIManager/` | Panel show/close, popup stack, fade |
| `ISceneLoaderSubsystem` | `Core/SceneLoader/` | Return to Lobby on match end |
| `IHttpServiceSubsystem` | `Core/HttpService/` | `/api/decks/{id}` deck detail fetch |
| `IAuthSessionSubsystem` + `IAuthSessionModel` | `Core/AuthSession/` | `UserId` for player labeling |
| `IAudioManagerSubsystem` | `Core/AudioManager/` | SFX on damage/skill |
| `INetworkManagerSubsystem` | `Core/Network/` | Active `NetworkRunner` access |
| `IBackendBridgeSubsystem` | `Core/BackendBridge/` | `ReportMatchResultAsync()` on end |
| `ICardLoadingManagerSubsystem` | `Core/CardLoadingManager/` | `TryGetCardData`, `TryGetSkillData`, `TryGetEffectData`, behavior SO lookup |

### 1.2 Lobby subsystems consumed by Gameplay

| Subsystem | Interface path | Why |
|---|---|---|
| `IProfileSubsystem` | `Core/Scripts/Interfaces/Features/Lobby/Profile/` | Player name, avatar, level — displayed on `Profile_Player`. Marked for Core relocation later; design Gameplay to inject only the interface so the move is invisible. |

> **`IDeckSubsystem` is NOT injected into any Gameplay script.** `IGameplayDeckSubsystem` (in `Core/Scripts/Interfaces/Features/Gameplay/StartPhase/`) is the Gameplay-owned interface that calls `/api/decks` directly. `DeckSummaryData` (in `Core.Interfaces`, autoReferenced) and `DeckButton` (in `LobbyFeatures`) are type/UI references only — `GameplayFeatures.asmdef` needs the `LobbyFeatures` GUID for `DeckButton`.

### 1.3 In-progress Gameplay subsystems

| Stack | Status | Owner |
|---|---|---|
| `GameplayDeckChoose*` (Model/Controller/Subsystem/NetworkView/StateData/Bridge) | ~80% built. Implementation exists under `Features/Gameplay/Scripts/DeckChoose/`. Bound in `GameplayInstaller`. Interfaces need to move to `Core/Scripts/Interfaces/Features/Gameplay/StartPhase/`. Prefab wiring + spawn trigger pending. | Track B (panel wiring) + Track A (spawn trigger from phase machine) |
| `GameplayDeckSubsystem` | Loads the player's decks from `/api/decks`. Already bound. | Track B (consumes) |
| `GameplayDeckChoosePanel` | MonoBehaviour exists at `Features/Gameplay/Scripts/UI/`. Needs prefab wire-up on `PhaseInteractionPanel_DeckChoose.prefab`. | Track B |

### 1.4 UI prefabs already authored (under `Features/Gameplay/UI/`)

From `Gameplay_UI_Panels_details.md`:

- `Layout/Layout_Fullscreen_Gameplay.prefab` → root, needs `GameplayHUDController`
- `Component/Profile_Gameplay.prefab` → needs `GameplayPlayerProfileUI`
- `Component/PhaseInteractionPanel_DeckChoose.prefab` → needs `GameplayDeckChoosePanel`
- `Component/PhaseInteractionPanel_DrawCard.prefab` → needs `DrawPhasePanel`
- `Component/PhaseInteractionPanel_Fusion.prefab` → needs `FusionPanel`
- `Component/PhaseInteractionPanel_Hand.prefab` → needs `HandPanel` (drawer wrap by `HandPanelAnchor`)
- `Component/PhaseInteractionPanel_Skill.prefab` → needs `SkillPanel` (drawer wrap by `SkillPanelAnchor`)
- `Component/PhaseInteractionPanel_TurnOrder.prefab` → needs `TurnOrderPanel` (drawer wrap by `TurnOrderPanelAnchor`)
- `Component/PhaseInteractionPanel_MatchResult.prefab` → needs `MatchResultPanel`
- `Component/Overlay_Gameplay_Decks.prefab` → needs `GameplayDeckSelectOverlay` (8-slot grid)
- `Component/PhaseInteractionPanel_ChooseAChampion.prefab` → out of scope for v1 (Champion is inside deck per rulebook §3)

---

## 2. Architecture Commitments

The same 7-layer stack as `networked-subsystem-guideline.md`. For every gameplay feature in §4:

```
Panel (MonoBehaviour) ─► Subsystem facade ─► Controller (internal) ─► NetworkBridge.RPC()
                                                                       │
                                                                       ▼
                                                       [StateAuthority writes Networked props]
                                                                       │
                                                                       ▼
            Panel ◄── Subsystem event ◄── Model.Observable ◄── Model.ApplyState() ◄── NetworkView.Render() ◄── Render on all clients
```

### Hard rules carried over from the project CLAUDE.md

- `internal` on every `Model` and `Controller`. Only the `Subsystem` is `public`.
- All interfaces + `StateData` structs live in `Core/Scripts/Interfaces/Features/Gameplay/<domain>/`.
- DI: `Container.BindInterfacesAndSelfTo<Xxx>().AsSingle().NonLazy()` for all three layers in `GameplayInstaller`.
- Subsystem event handlers must never rethrow: wrap each `Event?.Invoke(...)` in try/catch and `Debug.LogException`.
- Stage input in controller; do not write to model until server confirms.
- `[Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]` for client→server intent.
- No `using UnityEditor;` in runtime scripts (wrap in `#if UNITY_EDITOR`).
- DOTween: use `DOTween.To(...)` for cross-assembly safety; transform `DOMove/DORotate` are fine.

### Scene & assembly facts

- `GameplayFeatures.asmdef` must reference `LobbyFeatures` GUID (already needed for `DeckButton`, `DeckSummaryData`). Add `Fusion.Runtime`, `Zenject`, `DOTween.Runtime`.
- `GameplayInstaller.cs` lives at `Features/Gameplay/DI/`. Currently has 4 DeckChoose bindings.
- The Gameplay scene's `SceneContext` references `GameplayInstaller`. `NetworkViewRegistry` (a SO referenced by Fusion) must register every Gameplay `NetworkBehaviour` prefab.

---

## 3. Frozen Contracts (define before splitting)

Both tracks block on these. Land them first as **interface-only commits** so the two members can work in parallel.

All paths under `Assets/_Game/Core/Scripts/Interfaces/Features/Gameplay/`.

### 3.1 GameState (phase machine)

```csharp
// GameState/IGameStateSubsystem.cs
public enum GameplayPhase { Setup, StartPhase, MainPhase, CombatPhase, DrawPhase, GameOver }

public struct GameStateData : INetworkStruct {
    public GameplayPhase Phase;
    public float PhaseTimeRemaining;
    public float MatchElapsed;
    public int RoundNumber;
    public PlayerRef CurrentCombatActor;
    [Networked, Capacity(4)] public NetworkArray<NetworkBool> PlayerReady => default;  // index by PlayerRef.PlayerId
}

public interface IGameStateSubsystem : ISubsystem {
    event UnityAction<GameplayPhase> PhaseChanged;
    event UnityAction<float> PhaseTimeRemainingChanged;
    event UnityAction<float> MatchElapsedChanged;
    event UnityAction<int> RoundNumberChanged;
    event UnityAction<PlayerRef> CurrentCombatActorChanged;
    event UnityAction<PlayerRef, bool> PlayerReadyChanged;
    event UnityAction AllPlayersReady;              // fires once when every active player is ready

    GameplayPhase Phase { get; }
    bool IsReady(PlayerRef p);
    bool AcceptsReadyInput { get; }                 // true during StartPhase/MainPhase/DrawPhase; false during Setup/CombatPhase/GameOver

    // LOCAL_INPUT_RPC — the only entry point for confirming the current phase.
    // Server validates AcceptsReadyInput and that PlayerRef matches RPC source.
    // Once PlayerReady[i]=true, RequestSetLocalReady(false) is rejected until the phase advances (locked).
    void RequestSetLocalReady(bool ready);

    void RegisterNetworkBridge(IGameStateNetworkBridge bridge);
    void OnAuthoritativeStateReceived(GameStateData data);
}
```

Phase advancement rules (server, in `GameStateController`):
- Server advances when `AllPlayersReady` fires **or** `PhaseTimeRemaining` reaches 0.
- On phase change, server resets `PlayerReady[i] = false` for every player.
- `AcceptsReadyInput` is false during `Setup` (deck not yet chosen), `CombatPhase` (advancement driven by `ICombatSubsystem` queue exhaustion), and `GameOver`.
- Once `PlayerReady[i]=true`, the server ignores further `RequestSetLocalReady(false)` RPCs from that player until the phase advances.
- `IGameStateSubsystem.PlayerReady[]` is the **only** ready flag in the system. No per-phase subsystem keeps its own ready state. Phase-confirm buttons call `RequestSetLocalReady(true)` after their payload RPC succeeds.

### 3.2 Board

```csharp
// Board/IBoardSubsystem.cs
public struct HexCoord : IEquatable<HexCoord> { public int P; public int Q; public int R => -P - Q; }

public interface IBoardSubsystem : ISubsystem {
    event UnityAction<IReadOnlyList<HexCoord>> TilesChanged;
    event UnityAction<HexCoord, NetworkId> TileOccupantChanged;
    event UnityAction<HexCoord, string> TileEffectChanged;

    IReadOnlyList<HexCoord> AllTiles { get; }
    Vector3 GetWorldPosition(HexCoord c);
    bool TryResolveWorldToHex(Vector3 world, out HexCoord c);
    int Distance(HexCoord a, HexCoord b);
    bool IsEmpty(HexCoord c);
    bool ContainsTile(HexCoord c);          // needed by HexPatternResolver ray-cast + stepped modes
    HexCoord GetDeployArea(PlayerRef owner);
}
```

### 3.3 Unit

```csharp
// Unit/UnitPublicData.cs  — always replicated to all clients
public struct UnitPublicData : INetworkStruct {
    public NetworkId UnitId; public PlayerRef Owner;
    public HexCoord Position; public int CurrentHP; public int MaxHP;
    public float Speed; public int DeathAnchor;
    public bool IsPersistent;
    public int GrowthStacks;
    [Networked, Capacity(8)] public NetworkArray<StatusSlot> StatusEffects => default;
}

// Unit/UnitPrivateData.cs  — replicated ONLY to Owner via AoI
public struct UnitPrivateData : INetworkStruct {
    public NetworkId UnitId; public PlayerRef Owner;
    // [0]=Move (BaseCD=1), [1]=NormalAttack (BaseCD=1), [2-5]=EquipSkills
    [Networked, Capacity(6)] public NetworkArray<SkillSlot> Skills => default;
}

public interface IUnitSubsystem : ISubsystem {
    event UnityAction<NetworkId> UnitSpawned;
    event UnityAction<NetworkId> UnitDied;
    event UnityAction<NetworkId, int> UnitHPChanged;
    event UnityAction<NetworkId, HexCoord> UnitMoved;
    event UnityAction<NetworkId, string, int> StatusApplied;
    event UnityAction<NetworkId, string> StatusRemoved;
    // owner-only event — fires only on the unit-owner's client
    event UnityAction<NetworkId, IReadOnlyList<SkillSlot>> OwnUnitSkillsChanged;

    IReadOnlyList<NetworkId> AllUnits { get; }
    bool TryGetPublic(NetworkId id, out UnitPublicData data);
    bool TryGetOwnSkills(NetworkId id, out IReadOnlyList<SkillSlot> skills);   // empty for non-owners
}
```

> Unit state is split across two NetworkObjects per spawned unit. `UnitPublicNetworkView` holds public combat data (position, HP, status effects) and is always-replicated. `UnitPrivateNetworkView` holds the skill list (IDs, cooldowns, one-time flags) and is AoI-restricted to the owning player via `Runner.SetPlayerAlwaysInterested(unitOwner, unitPrivateObject, true)`. The `SkillPanel` UI subscribes to `OwnUnitSkillsChanged` — when the current actor belongs to the opponent, the panel shows a placeholder.
>
> **Topology**: per-entity always-replicated (`UnitPublicNetworkView`) + per-entity AoI-restricted (`UnitPrivateNetworkView`). See §5.2.1 for registration rules.

### 3.4 Combat (queue + turn)

```csharp
// Combat/CombatQueueEntry.cs  — entry in the action queue; carries both identity and display data
public struct CombatQueueEntry {
    public NetworkId UnitId;
    public string CardId;   // unit card id — used by TurnOrderPanel to fetch the card image
}

// Combat/CombatStateData.cs  — ALL_CLIENTS via CombatNetworkView
public struct CombatStateData : INetworkStruct {
    public NetworkId CurrentActor;
    public NetworkBool HasMoved;   // true once RequestMove() resolves this turn
    public NetworkBool HasActed;   // true once any RequestNormalAttack() or RequestSkill() resolves this turn
    // ActionQueue lives as a separate [Networked, Capacity(N)] NetworkArray on CombatNetworkView
}

// Combat/ICombatSubsystem.cs
public interface ICombatSubsystem : ISubsystem {
    event UnityAction<IReadOnlyList<CombatQueueEntry>> QueueChanged;
    event UnityAction<NetworkId> CurrentTurnChanged;
    event UnityAction TurnEnded;

    IReadOnlyList<CombatQueueEntry> ActionQueue { get; }
    NetworkId CurrentActor { get; }
    bool CurrentActorCanMove { get; }   // !HasMoved — read on CurrentTurnChanged or OwnUnitSkillsChanged
    bool CurrentActorCanAct { get; }    // !HasActed — same

    void RequestMove(NetworkId unit, HexCoord destination);
    void RequestNormalAttack(NetworkId unit, HexCoord target);
    void RequestSkill(NetworkId unit, string skillId, HexCoord target);
    void EndTurn();   // ends the current actor's turn; call before using any slot = Skip Turn
}
```

> **Two-layer constraint.** Two independent mechanisms enforce the action rules:
> - **Per-slot CD (cross-turn):** prevents reusing the *same* skill slot before its cooldown expires. Move[0] and NormalAttack[1] have BaseCD=1 so their CD ticks 1→0 at the next turn start — always available again next turn.
> - **HasMoved / HasActed (within-turn, data only):** prevents using *multiple slots from the same category* in one turn. `HasActed=true` blocks all Act slots [1-5] for the rest of the turn even if their individual CDs are 0. These are plain data fields in `CombatStateData` — no dedicated change events are fired. The SkillPanel reads `CurrentActorCanMove` / `CurrentActorCanAct` synchronously on `CurrentTurnChanged` and `OwnUnitSkillsChanged` to recalculate button interactability. There is no UI element that displays "moved" or "acted" state — slot graying is the only effect.
>
> A slot is interactable only when both pass: `CurrentActorCanAct == true` (HasActed=false) **AND** `Skills[i].CurrentCD == 0`. `HasMoved` is technically redundant with `Skills[0].CD > 0` but is kept in `CombatStateData` for symmetry and to avoid deriving UI state from the AoI-restricted `UnitPrivateNetworkView`.
```

### 3.5 PlayerRoster (public profile) + PlayerCardZone (private hand)

Per-player data is split into two concerns. `IPlayerRosterSubsystem` is the single source of truth for public profile data that every widget subscribes to. `IPlayerCardZoneSubsystem` is owner-private cards-only — no public counts, no Deck/Discard replication.

```csharp
// PlayerRoster/PlayerRosterPublicData.cs  — always replicated to all clients
public struct PlayerRosterPublicData : INetworkStruct {
    public PlayerRef Owner;
    public int HP;
    public NetworkString<_32> PlayerName;
    public NetworkString<_32> UserId;          // for HTTP avatar fetch per-client
}

public interface IPlayerRosterSubsystem : ISubsystem {
    event UnityAction<PlayerRef, int> HPChanged;
    event UnityAction<PlayerRef, string> NameChanged;
    event UnityAction<PlayerRef, string> UserIdChanged;

    IReadOnlyList<PlayerRef> AllPlayers { get; }
    int GetHP(PlayerRef p);
    string GetName(PlayerRef p);
    string GetUserId(PlayerRef p);

    void RegisterNetworkBridge(IPlayerRosterNetworkBridge bridge);
    void OnAuthoritativeStateReceived(PlayerRosterPublicData data);
}
```

> **PlayerRoster** is a thin public facade for per-player profile data. `PlayerRosterPublicNetworkView` is one NetworkObject per player, always-replicated. HP drives `Profile_Gameplay.HPValueText` and `MatchResultPanel`; PlayerName drives `NameValueText` everywhere; UserId drives the local HTTP avatar fetch in both panels.
>
> **Topology**: per-player always-replicated. Canonical verified reference — see §5.2.1 for registration rules. **PlayerCardZone** is per-player AoI-restricted — same rules, add `SetPlayerAlwaysInterested`.

```csharp
// PlayerCardZone/PlayerCardZonePrivateData.cs  — replicated ONLY to Owner via AoI
public struct PlayerCardZonePrivateData : INetworkStruct {
    public PlayerRef Owner;
    [Networked, Capacity(6)] public NetworkArray<NetworkString<_32>> Hand => default;
    // Deck and Discard are server-only — no [Networked] mirror. No UI consumes them.
}

public interface IPlayerCardZoneSubsystem : ISubsystem {
    // fires on all clients but carries the owning PlayerRef — handlers filter by local player
    event UnityAction<PlayerRef, IReadOnlyList<string>> HandChanged;

    IReadOnlyList<string> GetHand(PlayerRef player);   // returns the given player's hand; non-owners get an empty list

    // Server intents (LOCAL_INPUT_RPC)
    void RequestDraw(PlayerRef p, int count);
    void RequestKeepCards(PlayerRef p, IReadOnlyList<string> keep);
    void RequestPlayMainPhaseSpell(string cardId, HexCoord target);
}
```

> **PlayerCardZone** is owner-private. Hand lives on `PlayerCardZonePrivateNetworkView` — a separate NetworkObject per player, made visible only to its `Owner` via `Runner.SetPlayerAlwaysInterested(owner, privateObject, true)`. Deck and Discard are server-side state inside `PlayerCardZoneModel`; they are never `[Networked]` because no UI consumes them. The subsystem only exposes `OwnHandChanged` — opponents have no hand events at all.

### 3.6 Fusion

```csharp
// Fusion/IFusionSubsystem.cs
public struct FusionStagingData {
    public string ChampionOrTroopId;
    public string[] EquipSpellIds;  // up to 3 or 4 depending on innate skill
}

public interface IFusionSubsystem : ISubsystem {
    event UnityAction<FusionStagingData> StagingChanged;
    event UnityAction FusionConfirmed;

    void StageBase(string troopOrChampionId);
    void StageEquipSpell(int slotIndex, string equipSpellId);
    void ClearStaging();
    Task ConfirmFusion();
}
```

### 3.7 Targeting

```csharp
// Targeting/ITargetingSubsystem.cs
[Flags] public enum TargetMask { None = 0, Enemy = 1, Ally = 2, EmptyTile = 4, Self = 8 }
// None (0) = self-only; no tile selection. TargetingSubsystem treats Mask == None as "apply to caster's own tile".

public struct TargetingRequest {
    public TargetMask Mask;
    public int Range;           // computed by HexPatternResolver.GetRange(skillData.target_pattern) in SkillPanel
    public string DisplayPattern; // skill string_id — used by TargetingSubsystem to look up display_pattern list
    public NetworkId Caster;    // TargetingSubsystem resolves position via IUnitSubsystem.TryGetPublic(Caster)
    public bool IgnorePathfinding; // from skillData.ignore_pathfinding; passed through to RequestMove/RequestSkill
}

public interface ITargetingSubsystem : ISubsystem {
    event UnityAction<TargetingRequest> TargetingStarted;
    event UnityAction<IReadOnlyList<HexCoord>> HighlightedTilesChanged;
    event UnityAction TargetingCancelled;

    void BeginTargeting(TargetingRequest req, UnityAction<HexCoord> onConfirmed);
    void Cancel();
}
```

> **`TargetingSubsystem` injections**: injects `IBoardSubsystem`, `IUnitSubsystem`, `ICardLoadingManagerSubsystem`.
> `RefreshRangeHighlights` (called on `BeginTargeting`): looks up caster position via `IUnitSubsystem.TryGetPublic(req.Caster)`, then calls `IBoardSubsystem.GetTilesInRange(casterPos, req.Range)`.
> `HoverTile`: looks up skill data via `ICardLoadingManagerSubsystem.TryGetSkillData(req.DisplayPattern)`, resolves AoE via `HexPatternResolver.ResolveAll(hoveredTile, skillData.display_pattern, board)`, fires `HighlightedTilesChanged`.
>
> **`HexPatternResolver`** (static utility, `Features/Gameplay/Scripts/Targeting/HexPatternResolver.cs`): converts GDS `List<HexCoordinate>` `{n,p,q}` entries into resolved `List<HexCoord>` board tiles. `GetRange` returns the max `n` from a target_pattern list (used by `SkillPanel` before populating `req.Range`). `ResolveAll` unions all entries; `Resolve` dispatches each entry by the discriminator table in `F4-targeting-hexpattern.md`.

### 3.8 TileEffect

```csharp
// TileEffect/ITileEffectSubsystem.cs
public struct TileEffectInstance : INetworkStruct {
    public HexCoord Position;
    public NetworkString<_32> EffectId;
    public int DurationRemaining;
    public PlayerRef Owner;
}

public interface ITileEffectSubsystem : ISubsystem {
    event UnityAction<TileEffectInstance> EffectApplied;
    event UnityAction<HexCoord> EffectRemoved;
    bool TryGet(HexCoord c, out TileEffectInstance instance);
}
```

### 3.9 MatchResult

> **Name change from original plan**: `MatchResultData` was renamed to `GameMatchResult` to avoid a collision with the existing `MatchResultData` class in `Core/Scripts/Models/APIModels.cs` (used by BackendBridge for HTTP serialization).

```csharp
// MatchResult/GameMatchResult.cs  (plain C# struct — public match data, not INetworkStruct)
public struct GameMatchResult {
    public PlayerRef Winner;
    public bool IsTie;
    public float DurationSeconds;
}

// MatchRewards/MatchRewardsPrivateData.cs  — replicated ONLY to Owner via AoI
public struct MatchRewardsPrivateData : INetworkStruct {
    public PlayerRef Owner;
    public int GoldEarned;
    public int XPEarned;
}

public interface IMatchResultSubsystem : ISubsystem {
    event UnityAction<GameMatchResult> MatchEnded;
    bool HasResult { get; }
    GameMatchResult Result { get; }
    Task ReturnToLobby();
    void RegisterNetworkBridge(IMatchResultNetworkBridge bridge);
    void OnAuthoritativeStateReceived(GameMatchResult data);
}

public interface IMatchRewardsSubsystem : ISubsystem {
    // owner-only event — fires only on the owning client after AoI replication
    event UnityAction<int, int> OwnRewardsReceived;   // (gold, xp)
    int OwnGold { get; }
    int OwnXP { get; }
    void RegisterNetworkBridge(IMatchRewardsPrivateNetworkBridge bridge);
    void OnAuthoritativeStateReceived(MatchRewardsPrivateData data);
}
```

> **Topology**: `MatchRewardsPrivateNetworkView` is per-player AoI-restricted. See §5.2.1.
>
> **Match-end shutdown flow.** Server writes `GameMatchResult` (Winner, IsTie, DurationSeconds) as a `[Networked]` prop on `MatchResultNetworkView` — replicated to all clients. Server then writes per-player Gold/XP to each player's `MatchRewardsPrivateNetworkView` (AoI-restricted, so each client only receives its own). After both writes are committed, server calls `await IBackendBridgeSubsystem.ReportMatchResultAsync(...)`, then `Runner.Shutdown()`. Clients' `MatchResultPanel` subscribes to both events and caches the values locally so the panel remains populated after the runner disconnects. `Button_Confirm` calls `ISceneLoaderSubsystem.LoadScene("Lobby")` — a local-only operation with no network dependency.

### 3.10 Network bridges (one per subsystem)

For every subsystem above, an `IXxxNetworkBridge` with the RPC signatures it needs. These also live in `Core/Scripts/Interfaces/Features/Gameplay/<domain>/`. Track A implements the NetworkViews; Track B never sees them.

New/updated bridges introduced by the AoI split: `IPlayerRosterNetworkBridge` (HP/Name/UserId mutations on `PlayerRosterPublicNetworkView`), `IPlayerCardZonePrivateNetworkBridge` (hand mutations on the per-player private object; Deck/Discard never leave the server), `IUnitPublicNetworkBridge` (position/HP/status on `UnitPublicNetworkView`), `IUnitPrivateNetworkBridge` (skills/cooldowns on `UnitPrivateNetworkView`), `IMatchRewardsPrivateNetworkBridge` (Gold/XP writes on `MatchRewardsPrivateNetworkView`). Each private bridge is registered only with the StateAuthority's view of each owner's private object; AoI ensures it replicates only to the owner.

---

## 4. Rule Decomposition — Features × Components

Each row is **independently implementable and incrementally testable**. The "Components" column lists every script that must be touched. ★ marks the entry point Test step.

### Group F1 — Foundation (must land first)

| # | Feature | Rule ref | Components |
|---|---|---|---|
| F1.1 | **Scene bootstrap from Lobby** | — | `GameplayInstaller` (bindings); Gameplay scene's `NetworkSceneManagerDefault`; in Lobby `MatchMakingSubsystem` calls `_sceneLoader.LoadNetworkedScene(_runner, "Gameplay")`. Verify the `NetworkRunner` is configured for **Host mode with AreaOfInterest enabled** (or Shared mode) — `Runner.SetPlayerAlwaysInterested` is a no-op in the default replicate-everything topology. |
| F1.2 | **Hex board generation** | §1 | `BoardModel`, `BoardController`, `BoardSubsystem`, `BoardNetworkView`, `HexTile.prefab`. Generates r∈[-4,4], numCols=9-\|r\|, rotation Euler(270,330,0), spacing horizontal=1.732 / vertical=1.5 (or computed from tile Z-bounds). Deploy areas at (4,-4) and (-4,4). |
| F1.3 | **Phase machine + match timer** | §5 | `GameStateModel`, `GameStateController`, `GameStateSubsystem`, `GameStateNetworkView`. Durations: Start=30s, Main=60s, Draw=30s, MatchCap=3600s. |
| F1.4 | **HUD shell** | — | `GameplayHUDController` on `Layout_Fullscreen_Gameplay.prefab`. Wires `PhaseNameValueText`, `MatchTimeValueText`, two `Profile_*` slots. Hides `Profile_Enemy2` (3-player reserved). Must call `profileUI.Bind(playerRef)` on each slot so `GameplayPlayerProfileUI` knows which PlayerRef to filter events for — used for `PlayerReadyChanged` / `HPChanged` / `NameChanged` (all sourced from `IPlayerRosterSubsystem` and `IGameStateSubsystem`). |
| F1.5 | **Profile bridge to HUD** | — | `GameplayPlayerProfileUI` on `Profile_Gameplay.prefab`. Injects `IProfileSubsystem` (own PFP, local cache), `IPlayerRosterSubsystem` (HP / Name / UserId for all players), and `IGameStateSubsystem` (PlayerReady). Maps `HPChanged` → `HPValueText`, `NameChanged` → `NameValueText`, `PlayerReadyChanged` → `ReadyToggle` visual (always non-interactable), `UserIdChanged` → HTTP avatar fetch → `Panel` (PFP). Own PFP uses `IProfileSubsystem.ProfileChanged` directly. |

### Group F2 — Start Phase

| # | Feature | Rule ref | Components |
|---|---|---|---|
| F2.1 | **Deck selection per player** | §5 Start | Existing `GameplayDeckChoose*` stack. Finish: move 5 interface files to `Core/Scripts/Interfaces/Features/Gameplay/StartPhase/`. Wire `GameplayDeckChoosePanel` to `PhaseInteractionPanel_DeckChoose.prefab`. Wire `GameplayDeckSelectOverlay` to `Overlay_Gameplay_Decks.prefab` (8 slots, populated from `IGameplayDeckSubsystem.DecksChanged` — **not** `IDeckSubsystem`; `GameplayDeckSubsystem` calls `/api/decks` directly and is bound in `GameplayInstaller`). |
| F2.2 | **NetworkView spawn trigger** | §5 Start | `GameStateSubsystem.OnPhaseChanged(StartPhase)` → spawns one `GameplayDeckChooseNetworkView.prefab` per player via `Runner.Spawn(prefab, inputAuthority: player)`. |
| F2.3 | **Granted-cards shuffle + opening hand** | §3 Granted, §5 Start | `PlayerCardZoneController.SetupDeckForMatch(championId, supportCardIds)` — reads `CardLoadingManagerSubsystem.GetCardData(championId).grants_cards`, shuffles into the 20 supports, deals 6 via `RequestDraw(6)`. HP initialization (`champion.hp`) is a separate call to `PlayerRosterController.SetupForMatch(championId)` — player HP belongs to `IPlayerRosterSubsystem`, not `PlayerCardZone`. |
| F2.4 | **Auto-confirm Start on timer expiry** | §5 Start | `GameStateController` on phase-timer 0 calls `IGameplayDeckChooseSubsystem.AutoConfirmLastDeck()` for any unready player (commits a default deck payload), which routes through the standard `SubmitAsync` → `RequestSetLocalReady(true)` path. `PlayerReady[i]` flips as a result, not directly. |

### Group F3 — Main Phase

| # | Feature | Rule ref | Components |
|---|---|---|---|
| F3.1 | **Hand panel** | §13 | `HandPanel` MonoBehaviour on `PhaseInteractionPanel_Hand.prefab`. Subscribes `IPlayerCardZoneSubsystem.OwnHandChanged` (local player only). Renders into `CardSlot [×N]`. Drawer wrapped by `HandPanelAnchor.prefab` via `PanelDrawer`. **Visible during MainPhase** (card drag source for Fusion) **and DrawPhase** (card management / keep selection) — `PanelVisibilityRouter` shows `HandPanelAnchor` for both phases. |
| F3.2 | **Fusion staging UI** | §4 | `FusionPanel` on `PhaseInteractionPanel_Fusion.prefab`. **Direct phase panel — no PanelDrawer anchor; shown/hidden directly by `PanelVisibilityRouter` for `MainPhase`.** Wires `TimeValueText` (TMP_Text timer display — `IGameStateSubsystem.PhaseTimeRemainingChanged`), `UnitSlot`, `MovementSlot` (always shown; populates Skills[0] with `base_move` behavior, BaseCD=1, TargetMask=EmptyTile, Range=1 (adjacent hex, fixed for all units), AOE=center tile only), `NormalAttackSlot` (always shown; populates Skills[1] with `base_normal_attack` behavior, BaseCD=1, TargetMask/Range/DisplayPattern from card data), `FuseSlot1..4` (populates Skills[2-5]; one auto-occupied if base has `grants_skill`). `Button_Confirm` → `IFusionSubsystem.ConfirmFusion()`. |
| F3.3 | **Fusion authority** | §4, §1 | `FusionModel`, `FusionController`, `FusionSubsystem`, `FusionNetworkView`. Validates: ≤1 unit/turn, exactly 1 base, ≤4 slots, base's `grants_skill` occupies 1 slot if present. On confirm (drop-pod mechanic): `FusionNetworkView.SpawnUnit()` first calls `ServerClearDeployArea(owner)` — finds any unit on the deploy tile, applies its `death_anchor` to the owner's HP via `PlayerCardZoneNetworkView.ServerApplyDamage`, despawns it, removes any tile effect via `TileEffectSubsystem` — then spawns the fused `NetworkUnit` on the now-empty Deploy Area tile. Immediately after spawn: discard base Troop card and all EquipSpell cards via `PlayerCardZoneNetworkView.ServerDiscardFusionCard(cardId)`. **The Champion card is never discarded** — it is always available and must never enter the Discard Pile. |
| F3.7 | **Forfeit / disconnect** | §6 Win | Override `OnPlayerLeft(NetworkRunner, PlayerRef)` in `GameStateNetworkView`. On fire (server-authority only): iterate `IUnitSubsystem.AllUnits`, for each unit owned by the leaving player apply its `death_anchor` to the player's `PlayerCardZoneNetworkView.HP` and despawn it; then call `ServerCheckElimination()`. The remaining player is declared winner via the normal win-condition path. |
| F3.4 | **MainPhaseSpell play** | §3, §6 Main | `IPlayerCardZoneSubsystem.RequestPlayMainPhaseSpell(cardId, target)`. Server validates the player has **not yet confirmed** fusion (`PlayerReady[i] == false`); rejects if already confirmed. Routes to `BehaviorRegistrySubsystem.ResolveMainPhaseSpell(behaviorId).Execute(target)`. Card moves to discard immediately. |
| F3.5 | **Champion always-available in fusion** | §3 | `FusionPanel` shows Champion card pinned to base slot pool; not consumed from hand. |
| F3.6 | **Main-phase ready/confirm + auto-advance** | §6 Main | `Button_Confirm` calls `IFusionSubsystem.ConfirmFusion()` (payload RPC), then on success calls `IGameStateSubsystem.RequestSetLocalReady(true)`. Once `PlayerReady[i]=true`, the server rejects any further `RequestPlayMainPhaseSpell` RPCs from that player — the player's actions are locked until phase transitions. Timer-0 fallback: `GameStateNetworkView.HandlePhaseTimeout()` (MainPhase case) calls `AutoDeployUnfusedPlayers()` — iterates all players via `GameplayNetworkCoordinator`; for each player whose `FusionNetworkView.HasFusedThisTurn == false`, calls `fusionView.ServerAutoConfirmFusion(championId)` (Champion + 0 EquipSpells) — then transitions to CombatPhase. |

### Group F4 — Combat Phase

| # | Feature | Rule ref | Components |
|---|---|---|---|
| F4.1 | **Action queue build** | §5 Combat Step 1 | `CombatController.BuildQueue()` sorts all units by Speed desc → HP asc → coin toss. Mid-combat spawns appended via `CombatController.AppendToQueue(unitId)`. |
| F4.2 | **TurnOrder panel** | — | `TurnOrderPanel` on `PhaseInteractionPanel_TurnOrder.prefab`. Subscribes `ICombatSubsystem.QueueChanged`. Each `CombatQueueEntry` carries `UnitId` (for highlight on turn change) and `CardId` (looked up via `ICardLoadingManagerSubsystem` to render the unit card image). Spawns one card item per entry into `Content` RectTransform. Drawer-wrapped by `TurnOrderPanelAnchor`. |
| F4.3 | **Unit turn cycle** | §5 Combat Step 2 | `CombatController.AdvanceTurn()`. On enter: reset `HasMoved=false`, `HasActed=false` in `CombatStateData`; tick **all 6** skill CDs by 1 (Move[0] and N_Atk[1] tick 1→0; equip skills tick toward 0). After `RequestMove()` resolves: server sets `Skills[0].CurrentCD=1` and `HasMoved=true`. After `RequestNormalAttack()` or `RequestSkill([1-5])` resolves: server sets that slot's `CurrentCD=BaseCD` and `HasActed=true`. Both writes replicate via `CombatNetworkView.Render()` → no dedicated change events; SkillPanel re-evaluates button interactability on next `CurrentTurnChanged` or `OwnUnitSkillsChanged`. `EndTurn()` valid at any point (zero slots used = **Skip Turn**; partial = end early). Auto-end on no-input timer calls `EndTurn()`. |
| F4.4 | **Movement & pathfinding** | §6 Combat | `BoardSubsystem.FindPath(from, to)` walks empty tiles only. `ignore_pathfinding: true` skips intermediate checks (destination must be empty). Max distance = 1 (all units move to adjacent hex only; no per-unit move range stat). Knockback stops at board boundary. |
| F4.5 | **Skill panel + active skill use** | §15 | `SkillPanel` on `PhaseInteractionPanel_Skill.prefab` (drawer-wrapped). Shows all 6 skill slots: Move[0], NormalAttack[1], EquipSkills[2-5] as `CardSlot_Empty`. On `CurrentTurnChanged` or `OwnUnitSkillsChanged`: re-evaluate each slot — interactable only if `CurrentActorCanMove/Act == true` AND `Skills[i].CurrentCD == 0`. Click on an interactable slot: build `TargetingRequest` — `Range = HexPatternResolver.GetRange(skillData.target_pattern)`, `Mask = ResolveTargetMask(skillData.target_condition)`, `DisplayPattern = skillId` (if display_pattern present), `Caster = _currentActor` (NetworkId), `IgnorePathfinding = skillData.ignore_pathfinding`. Then `ITargetingSubsystem.BeginTargeting(req, ...)`. On confirm: Move[0] → `RequestMove()`, NormalAttack[1] → `RequestNormalAttack()`, EquipSkills[2-5] → `RequestSkill()`. |
| F4.6 | **Targeting display** | §9, §15 | `TargetingSubsystem` implements range ring via `GetTilesInRange(casterPos, req.Range)` and AoE hover preview via `HexPatternResolver.ResolveAll(hoveredTile, skillData.display_pattern, board)`. `TargetingOverlay` (Track B) renders: yellow = in-range tiles, green = valid hovered tile, red = invalid hovered tile. Bitmask: Enemy=1, Ally=2, EmptyTile=4, Self=8. `target_condition: 0` ⇒ self-only, `TargetMask.Self`, no tile selection loop. |
| F4.7 | **3-pass damage pipeline** | §8 | `DamagePipelineSubsystem.Resolve(action)`. Aggregate → Intercept (Tile effects first then Unit effects) → Commit. Hooks: `IInterceptor` list rebuilt per action from active statuses + tile effects. |
| F4.8 | **Status effects (ScriptableObject behaviors)** | §14 | `StatusEffectBehaviorBaseSO` base, concrete `GenericStatusEffectBehaviorSO`. All effectIds use `seb_` prefix matching GDS `status_effect_behavior_id`. Key behaviors: `seb_barkskin_ward` (full-block next damage instance, consumed on trigger), `seb_burning` (1 dmg/turn), `seb_melting` (2 dmg/turn), `seb_decay` (blocks heal), `seb_rooted` (prevents movement), `seb_burning_trail` (leaves `seb_scorching_ground` tile on move), growth-related (`seb_growth_stack`, `seb_ascendance`). 20 total SOs (7 existing updated + 13 new). |
| F4.9 | **Skill cooldowns & one-time** | §12 | `UnitController.OnTurnStart()` decrements **all 6 slots** (Move[0], NormalAttack[1], EquipSkills[2-5]) by 1. Move and N_Atk have `BaseCD=1` — they always tick 1→0 at turn start and are available every turn. `one_time: true` applies only to EquipSkills; `CombatNetworkView.ServerEndCombatPhase()` iterates `IUnitSubsystem.AllUnits` and for each `IsPersistent` unit calls `unitView.ServerResetOneTimeFlags()` — resetting all `SkillOneTimeDisabled[i] = false`. Non-persistent units are re-fused each cycle so their flags are always fresh. Persistent Unit CDs (including Move and N_Atk) carry into the next cycle — they are NOT reset on phase end. |
| F4.10 | **Tile effects (Lingering)** | §10 | `TileEffectSubsystem`. Corrupted/Seeded/Melting; one per tile (replaces). Survives board clear (but Deploy Area force-clear wipes). Owning player's units immune to own faction's negative effects. |
| F4.11 | **Friendly-fire & faction immunity** | §2, §11 | Hardcoded checks in `DamagePipelineSubsystem.Aggregate()`: skip allied tiles unless skill has `ignore_friendly_fire: true`. |
| F4.12 | **Death & DeathAnchor** | §6 Combat Step 3 | `UnitController.OnHPZero()` immediately destroys unit, subtracts `death_anchor` from owner's HP via `IPlayerRosterController` (player HP lives in `IPlayerRosterSubsystem`, not `PlayerCardZone`). `GameStateSubsystem.CheckElimination()` runs continuously **across all phases** — not only after combat commits. Any HP drop in any phase (e.g., Deploy Area drop-pod destroying a unit during Main Phase) triggers the same elimination check. Eliminated player's deployed + Persistent Units are all destroyed immediately. |
| F4.13 | **Persistent units** | §6 | `is_summonable: false` units spawned via skill. Marked `IsPersistent=true` on `UnitStateData`. Survive board clear. Cooldowns persist. Deploy Area still wipes. |
| F4.14 | **Verdant evolution** | §8 | `EvolutionBehaviorSO`. At 4 Growth Stacks → swap unit identity to next form (Seedling→Sapling→Young Treant→Thorn Colossus). Stacks reset to 0. Tracked on `UnitStateData.GrowthStacks`. Units outside the evolution chain that receive Growth Stacks cap at 4 (`max_stack`); no reset, no evolution fires. Thorn Colossus is final — 4-stack overflow is ignored. |
| F4.15 | **Board clear** | §6 Combat Step 4 | `CombatSubsystem.OnQueueExhausted()` when ≤1 player still has player-deployed units remaining (Persistent Units excluded from this check). All non-persistent → discard. Tile effects remain on board but freeze (no duration tick, no active effect) — inter-phase gap starts. Unit status effects on Persistent Units also freeze during inter-phase gap (no tick, no active effect); they unfreeze at the start of the next Main Phase. Deploy Area force-wiped by same drop-pod rules (see §1). |

### Group F5 — Draw Phase

| # | Feature | Rule ref | Components |
|---|---|---|---|
| F5.1 | **Draw 2 + hand-keep UI** | §5 Draw, §13 | `DrawPhasePanel` on `PhaseInteractionPanel_DrawCard.prefab`. Shows 2 new + current hand. Drag-and-drop keep selection. `Button_Confirm` → `IPlayerCardZoneSubsystem.RequestKeepCards(keep)`. Drops cards go to discard. Hand max=6. **`HandPanelAnchor` is also shown during DrawPhase** (so the player can see their current hand alongside this panel) — both are entries in `PanelVisibilityRouter._phasePanels[]` for `DrawPhase`. |
| F5.2 | **Reshuffle on empty deck** | §13 | `PlayerCardZoneController.DrawCard()`: if deck empty, shuffle Discard into Deck immediately, then draw. |
| F5.3 | **Draw-phase ready/confirm + auto-advance** | §5 Draw | `DrawPhasePanel.Button_Confirm` calls `IPlayerCardZoneSubsystem.RequestKeepCards(keep)` (card payload), then on success calls `IGameStateSubsystem.RequestSetLocalReady(true)` (phase advance). Timer-0 fallback: `GameStateController` flips remaining unready players. |

### Group F6 — Match End

| # | Feature | Rule ref | Components |
|---|---|---|---|
| F6.1 | **Win condition** | §5 Win | `GameStateSubsystem.CheckWinCondition()`: last alive wins. 1h cap → highest HP. Tie → all players Loss + penalty (flagged in `GameMatchResult`). |
| F6.2 | **Match result panel** | §5 Win | `MatchResultPanel` on `PhaseInteractionPanel_MatchResult.prefab`. Subscribes to `IMatchResultSubsystem.MatchEnded` (Winner, IsTie, DurationSeconds → ALL_CLIENTS) and `IMatchRewardsSubsystem.OwnRewardsReceived` (Gold/XP → OWNER_ONLY via AoI). Wires `Player0/1/2` slots (crown enabled only on winner, PFP fetched by UserId, name from `IPlayerRosterSubsystem`), `GoldValueText`, `XPValueText`, `TimeValueText`. Caches all values locally on arrival. `Button_Confirm` → `IMatchResultSubsystem.ReturnToLobby()` → `ISceneLoaderSubsystem.LoadScene("Lobby")` — works after runner shutdown. |
| F6.3 | **Backend report + shutdown** | — | Server-side flow: `MatchResultController.OnEnd()` writes `GameMatchResult` to `MatchResultNetworkView`, writes per-player Gold/XP to each `MatchRewardsPrivateNetworkView`, then calls `await IBackendBridgeSubsystem.ReportMatchResultAsync(...)`, then `Runner.Shutdown()`. Clients receive rewards via AoI replication before the shutdown completes. |

### Group F7 — Engine plumbing

| # | Feature | Rule ref | Components |
|---|---|---|---|
| F7.1 | **Behavior registry** | §14 | `BehaviorRegistrySubsystem`. Loads `GenericSkillBehaviorSO` / `StatusEffectBehaviorSO` / `MainPhaseSpellBehaviorSO` from Resources or `ICardLoadingManagerSubsystem`. Lookup by string id. Validation at asset load. |

---

## 5. Track A — Server Authority Engine

### 5.1 Files to create

All implementation files under `Features/Gameplay/Scripts/<Domain>/`. Interfaces under `Core/Scripts/Interfaces/Features/Gameplay/<Domain>/`.

| Domain folder | Files |
|---|---|
| `GameState/` | `GameStateModel.cs`, `GameStateController.cs`, `GameStateSubsystem.cs`, `GameStateNetworkView.cs` |
| `Board/` | `BoardModel.cs`, `BoardController.cs`, `BoardSubsystem.cs`, `BoardNetworkView.cs` |
| `Unit/` | `UnitModel.cs`, `UnitController.cs`, `UnitSubsystem.cs`, `UnitPublicNetworkView.cs`, `UnitPrivateNetworkView.cs` (one public + one private NetworkBehaviour per spawned unit) |
| `Combat/` | `CombatModel.cs`, `CombatController.cs`, `CombatSubsystem.cs`, `CombatNetworkView.cs`. Interface-side also requires `CombatQueueEntry.cs` in `Core/Scripts/Interfaces/Features/Gameplay/Combat/`. |
| `PlayerRoster/` | `PlayerRosterModel.cs`, `PlayerRosterController.cs`, `PlayerRosterSubsystem.cs`, `PlayerRosterPublicNetworkView.cs` (one per player, always-replicated) |
| `PlayerCardZone/` | `PlayerCardZoneModel.cs`, `PlayerCardZoneController.cs`, `PlayerCardZoneSubsystem.cs`, `PlayerCardZonePrivateNetworkView.cs` (one per player, AoI-restricted to owner) |
| `MatchRewards/` | `MatchRewardsModel.cs`, `MatchRewardsController.cs`, `MatchRewardsSubsystem.cs`, `MatchRewardsPrivateNetworkView.cs` (one per player, AoI-restricted to owner) |
| `Fusion/` | `FusionModel.cs`, `FusionController.cs`, `FusionSubsystem.cs`, `FusionNetworkView.cs` |
| `TileEffect/` | `TileEffectModel.cs`, `TileEffectController.cs`, `TileEffectSubsystem.cs`, `TileEffectNetworkView.cs` |
| `DamagePipeline/` | `DamagePipelineSubsystem.cs`, `IInterceptor.cs`, `IAggregator.cs`, `IInterceptResult.cs` |
| `BehaviorRegistry/` | `BehaviorRegistrySubsystem.cs`, `BehaviorRegistryController.cs`, `BehaviorRegistryModel.cs` |
| `MatchResult/` | `MatchResultModel.cs`, `MatchResultController.cs`, `MatchResultSubsystem.cs`, `MatchResultNetworkView.cs` |
| `Targeting/` | `TargetingSubsystem.cs` (injects `IBoardSubsystem`, `IUnitSubsystem`, `ICardLoadingManagerSubsystem`), `HexPatternResolver.cs` (static utility — GDS `{n,p,q}` → board `HexCoord` list) |
| `ScriptableObjects/` | (re-author from LEGACY) `GenericSkillBehaviorSO.cs`, `StatusEffectBehaviorSO.cs`, `MainPhaseSpellBehaviorSO.cs`, `EvolutionBehaviorSO.cs` |

### 5.2 Prefabs to create / reuse

| Prefab | Type | Notes |
|---|---|---|
| `BoardManager.prefab` | NetworkObject + GameObjectContext | Holds `BoardNetworkView`. References `IM_Tile.prefab` instance. |
| `IM_Tile.prefab` | GameObject | One instance per hex. From LEGACY (visuals reusable). |
| `GameStateManager.prefab` | NetworkObject | `GameStateNetworkView`. Spawned by host on scene start. |
| `PlayerRosterPublicState.prefab` | NetworkObject | `PlayerRosterPublicNetworkView`. One per player. Always-replicated. |
| `PlayerCardZonePrivateState.prefab` | NetworkObject | `PlayerCardZonePrivateNetworkView`. One per player. Host calls `SetPlayerAlwaysInterested(owner, this, true)` on spawn; never adds other players' interest. |
| `UnitNetworkView.prefab` | NetworkObject | `UnitNetworkView`. All unit data (HP / position / owner / status effects / skills). One per spawned unit, visible to all clients. Has a `_meshRoot` child Transform; `Render()` instantiates the per-player mesh prefab and applies the material from `GameplayNetworkCoordinator._playerPieceConfigs[playerIndex]` once `Owner` is known (guarded by `_meshApplied`). AoI split into public/private NetworkViews deferred. |
| `MatchRewardsPrivateState.prefab` | NetworkObject | `MatchRewardsPrivateNetworkView`. One per player. Host writes Gold/XP at match-end; AoI restricted to the owner. Owning client caches the values locally so they survive runner shutdown. |
| `TileEffectInstance.prefab` | NetworkObject | One per applied effect. |
| `CombatCoordinator.prefab` | NetworkObject | Singleton, `CombatNetworkView`. |
| `MatchResultCoordinator.prefab` | NetworkObject | Singleton, `MatchResultNetworkView`. |

Register every prefab in `NetworkViewRegistry` SO referenced from the scene.

### 5.2.1 Per-player & per-entity NetworkView requirements

Any NetworkView spawned more than once per peer (marked per-player or per-entity above)
must follow the full per-player rules in `networked-subsystem-guideline.md §Topology`.
Quick summary:

- **Bridge registration**: subsystem + controller use `RegisterNetworkBridge(PlayerRef owner, IXxxNetworkBridge bridge)`; controller holds `Dictionary<PlayerRef, IXxxNetworkBridge>`. Every replicated view calls `RegisterNetworkBridge(Object.InputAuthority, this)` unconditionally in `Spawned()` — no `HasInputAuthority` gate needed.
- **`PushState()` guards `Owner != PlayerRef.None`** — the first `Spawned()` call may fire before the pre-spawn callback sets `Owner`.
- **AoI-restricted views**: call `Runner.SetPlayerAlwaysInterested(Owner, Object, true)` in `Spawned()` when `HasStateAuthority`.
- **Cleanup**: `Despawned()` calls `RegisterNetworkBridge(_cachedInputAuthority, null)`.
- **Spawn site**: pass `Owner` and any initial state via `Runner.Spawn` pre-spawn callback.
- **Server-side writes** (e.g. HP from damage): the server calls bridge methods directly on the view reference it holds (from coordinator dicts), NOT via the subsystem's bridge dict. `HasStateAuthority` guard inside each method makes it a no-op on non-authority peers.

Canonical reference: `PlayerRosterPublicNetworkView.cs` (always-replicated, verified).
AoI-restricted views follow the same shape plus the `SetPlayerAlwaysInterested` call;
no AoI-restricted view has been end-to-end verified yet — treat as unproven until tested.

> **Context**: the HUD name-sync failures fixed in commits `7d10bb1 / fc6c7ab / 1f7913b`
> were caused by MPPM PlayerPrefs sharing (see §5.4) combined with an older single-bridge
> architecture that made debugging non-deterministic. Both issues are now addressed.

### 5.3 DI bindings to add in `GameplayInstaller.cs`

```csharp
// Foundation
Container.BindInterfacesAndSelfTo<GameStateModel>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<GameStateController>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<GameStateSubsystem>().AsSingle().NonLazy();

Container.BindInterfacesAndSelfTo<BoardModel>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<BoardController>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<BoardSubsystem>().AsSingle().NonLazy();

Container.BindInterfacesAndSelfTo<UnitModel>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<UnitController>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<UnitSubsystem>().AsSingle().NonLazy();

Container.BindInterfacesAndSelfTo<CombatModel>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<CombatController>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<CombatSubsystem>().AsSingle().NonLazy();

Container.BindInterfacesAndSelfTo<PlayerRosterModel>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<PlayerRosterController>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<PlayerRosterSubsystem>().AsSingle().NonLazy();

Container.BindInterfacesAndSelfTo<PlayerCardZoneModel>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<PlayerCardZoneController>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<PlayerCardZoneSubsystem>().AsSingle().NonLazy();

Container.BindInterfacesAndSelfTo<MatchRewardsModel>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<MatchRewardsController>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<MatchRewardsSubsystem>().AsSingle().NonLazy();

Container.BindInterfacesAndSelfTo<FusionModel>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<FusionController>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<FusionSubsystem>().AsSingle().NonLazy();

Container.BindInterfacesAndSelfTo<TileEffectModel>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<TileEffectController>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<TileEffectSubsystem>().AsSingle().NonLazy();

Container.BindInterfacesAndSelfTo<DamagePipelineSubsystem>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<BehaviorRegistryModel>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<BehaviorRegistryController>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<BehaviorRegistrySubsystem>().AsSingle().NonLazy();

Container.BindInterfacesAndSelfTo<MatchResultModel>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<MatchResultController>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<MatchResultSubsystem>().AsSingle().NonLazy();
```

### 5.4 Entry point (testable from Lobby)

**Lobby button:** "Battle" → `MatchMakingSubsystem.HostMatch()` → Network runner starts (Host mode), 1 mock 2nd player joined locally for dev → `_sceneLoader.LoadNetworkedScene(_runner, "Gameplay")`.

For two-instance testing without real matchmaking, add a **dev-only** "Host Test Match" and "Join Test Match" pair on the Lobby Battle screen (gated behind `#if UNITY_EDITOR || DEVELOPMENT_BUILD`). They use a fixed session name `"primora-dev-match"`.

> **⚠ MPPM PlayerPrefs sharing.** Unity's Multiplayer Play Mode package shares `PlayerPrefs`
> across all virtual player instances on the same machine. Any read from `PlayerPrefs` —
> `IAuthSessionSubsystem.UserId`, cached `IProfileSubsystem.Username`, auth tokens — returns
> the **last-logged-in instance's** value in every virtual player. Both players appear to be
> the same user. Symptoms: both HUD name slots show the same name; identity RPCs push
> duplicate strings; HUD sync looks broken even though the network code is correct.
>
> **Use two separate Editor instances** (ParrelSync or a second checkout) — not MPPM virtual
> players — for any test that involves per-player identity. MPPM is fine for non-identity
> testing (phase machine, combat, board generation). Running one Editor + one standalone build
> also works (separate PlayerPrefs namespaces).

### 5.5 Verification steps

1. **Phase loop heartbeat:** Open two Editor instances → Host + Client. Both transition `Setup → StartPhase`. Console logs from `GameStateSubsystem.PhaseChanged` fire on both sides within one Render() tick. Expand: progress through all 5 phases by clicking Confirm/auto-timeout.
2. **Board generation:** Visual check — 61 tiles in hexagonal layout (rows of 5,6,7,8,9,8,7,6,5 = 61). Deploy areas highlighted on (4,-4) and (-4,4).
3. **Unit spawn flow:** Skip to MainPhase → `IFusionSubsystem.StageBase("troop_warrior")` → ConfirmFusion → unit appears on Deploy Area for both clients.
4. **Combat queue:** Spawn 2 units (one per player) with different Speeds → CombatPhase → log shows correct order.
5. **Damage pipeline:** Use `troop_warrior` with normal attack on enemy adjacent → `UnitHPChanged` fires on both clients. Verify Aggregate→Intercept→Commit log order.
5b. **Hand privacy:** Two Editor instances, Host + Client. Inspect Client's Runner: the host player's `PlayerCardZonePrivateNetworkView` is not in the Client's interest set. Add a debug log to `PlayerCardZonePrivateNetworkView.Render()` printing the Hand contents — log fires only on the owning client, never on the opponent.
5b-2. **Skill privacy:** Spawn a unit on Host. On Client, inspect that unit's `UnitPrivateNetworkView` — its Skills array is empty (no AoI interest). Open `PhaseInteractionPanel_Skill` drawer when the host's unit is the current actor on the Client: panel renders the "opponent unit" placeholder, never the skill IDs.
5b-3. **Rewards privacy:** Trigger match end with Host as winner. Each client's `MatchRewardsPrivateNetworkView` is only accessible by its own owner. Host's Gold/XP fields are non-zero locally; Client's reflect their own rewards, not the host's.
5b-4. **Match-end shutdown survives the panel:** Server log order: `ReportMatchResultAsync` completes → `Runner.Shutdown()` invoked. On both clients, after the disconnect log fires, `PhaseInteractionPanel_MatchResult` remains fully populated (winner crown, names, time, owner's Gold/XP). Clicking `Button_Confirm` triggers `SceneLoader.LoadScene("Lobby")` with no network calls.
5c. **Ready handshake via Confirm button:** Two **separate Editor instances** (not MPPM — see §5.4 MPPM warning). Enter StartPhase. On Host, pick a deck and click `Button_Confirm` — `PlayerReady[host]` flips true on both clients; both `ReadyToggle` visuals flip on for the host's slot. On Client, do the same — `AllPlayersReady` fires; phase advances to MainPhase on both within one Render() tick.
5d. **Toggle non-interactable on every slot:** During any phase, attempt to click `ReadyToggle` on `Profile_Player` or `Profile_Enemy1` — neither responds (interactable=false); no RPC fires; `PlayerReady[]` unchanged.
5e. **Ready locked once true:** During StartPhase, after pressing Confirm, send a synthetic `RequestSetLocalReady(false)` — server rejects it; `PlayerReady[localPlayer]` remains true until phase advances.
5f. **AcceptsReadyInput coverage:** In Setup phase, `IGameStateSubsystem.AcceptsReadyInput` returns false. Same in CombatPhase and GameOver. Returns true in StartPhase / MainPhase / DrawPhase.
6. **Tile effect persistence across cycles:** Apply `corrupted` via skill → end CombatPhase → tile effect remains visible on `TileEffectInstance` prefab in next MainPhase.
7. **Death + DeathAnchor:** Kill a unit with `death_anchor=5` → owning player's HP drops by 5 (visible on `Profile_Player.HPValueText` via `IPlayerRosterSubsystem.HPChanged`).
8. **Match end:** Reduce one player's HP to 0 → `GameMatchResult.Winner` fires → `ReturnToLobby` after Confirm.

---

## 6. Track B — Player UX

### 6.1 Files to create

All under `Features/Gameplay/Scripts/UI/`. Interfaces under `Core/Scripts/Interfaces/Features/Gameplay/UI/` (for the panel-side state structs only — most events come from Track A's subsystems).

| File | Prefab it lives on | Subscribes to |
|---|---|---|
| `GameplayHUDController.cs` | `Layout_Fullscreen_Gameplay.prefab` | `IGameStateSubsystem` (PhaseChanged, MatchElapsedChanged) |
| `GameplayPlayerProfileUI.cs` | `Profile_Gameplay.prefab` | `IProfileSubsystem` (own PFP) + `IPlayerRosterSubsystem` (HP / Name / UserId for all players) + `IGameStateSubsystem` (PlayerReady display) |
| `GameplayDeckChoosePanel.cs` (finish) | `PhaseInteractionPanel_DeckChoose.prefab` | `IGameplayDeckChooseSubsystem`, `IGameplayDeckSubsystem` |
| `GameplayDeckSelectOverlay.cs` | `Overlay_Gameplay_Decks.prefab` | `IGameplayDeckSubsystem.DecksChanged` |
| `DrawPhasePanel.cs` | `PhaseInteractionPanel_DrawCard.prefab` | `IPlayerCardZoneSubsystem.HandChanged` (filters by local player) |
| `FusionPanel.cs` | `PhaseInteractionPanel_Fusion.prefab` | `IFusionSubsystem.StagingChanged`, `IFusionSubsystem.FusionConfirmed`, `IGameStateSubsystem.PhaseChanged`, `IGameStateSubsystem.PhaseTimeRemainingChanged` |
| `HandPanel.cs` | `PhaseInteractionPanel_Hand.prefab` (drawer-wrapped) | `IPlayerCardZoneSubsystem.HandChanged` (filters by local player) — visible in **MainPhase** (drag source) and **DrawPhase** (card management) |
| `SkillPanel.cs` | `PhaseInteractionPanel_Skill.prefab` | `ICombatSubsystem.CurrentTurnChanged`, `ICombatSubsystem.TurnEnded`, `IUnitSubsystem.OwnUnitSkillsChanged`. On each trigger: reads `CurrentActorCanMove`, `CurrentActorCanAct`, and each `Skills[i].CurrentCD` to set slot interactability. No dedicated HasMoved/HasActed events. `Button_SkipTurn` → `ICombatSubsystem.EndTurn()` (always enabled while it is the local player's unit's turn). |
| `TurnOrderPanel.cs` | `PhaseInteractionPanel_TurnOrder.prefab` | `ICombatSubsystem.QueueChanged` — each `CombatQueueEntry.CardId` used to fetch card image via `ICardLoadingManagerSubsystem` |
| `MatchResultPanel.cs` | `PhaseInteractionPanel_MatchResult.prefab` | `IMatchResultSubsystem.MatchEnded`, `IMatchRewardsSubsystem.OwnRewardsReceived`, `IPlayerRosterSubsystem` (names / UserIds for PFP fetch) |
| `TargetingOverlay.cs` | Spawned at runtime as overlay UI / world-space tile decorator | `ITargetingSubsystem.TargetingStarted`, `HighlightedTilesChanged` |
| `TileHighlight.cs` | MonoBehaviour on `TileHighlight.prefab` — owns `[SerializeField] Renderer _renderer`, exposes `SetColor(Color)`. Instantiated per-tile by `TargetingOverlay`; root is the position pivot, child `Mesh` holds the hex mesh + transparent material | local only |
| `CardDragHandle.cs` | Helper component on `CardSlot` prefabs for drag-and-drop into Fusion slots | local |
| `PanelVisibilityRouter.cs` | Empty GameObject in scene | `IGameStateSubsystem.PhaseChanged` → toggle which phase panel is active |

### 6.2 Prefabs to wire (no new prefabs — only component-attach + serialized field assignment)

For each prefab listed in §1.4, drop the matching `*.cs` script onto its root and serialize-field-assign children per `Gameplay_UI_Panels_details.md`. Use the existing `Tools/Primora/Add PanelDrawers to Anchors` editor menu to (re)wire drawer toggles on Hand/Skill/TurnOrder anchors. **Drawer-wrapped panels** (Hand, Skill, TurnOrder) use their anchor root (`HandPanelAnchor`, `SkillPanelAnchor`, `TurnOrderPanelAnchor`) as the `PanelVisibilityRouter` target. **Direct phase panels** (DeckChoose, Fusion, DrawCard, MatchResult) are router targets themselves — no anchor wrapper. `SkillPanelAnchor` is **CombatPhase only**; `HandPanelAnchor` is shown in both **MainPhase** and **DrawPhase**.

### 6.3 Profile-to-Core preparation (in scope for Track B, not the migration itself)

To keep Gameplay agnostic of where Profile lives:
- `GameplayPlayerProfileUI` injects **only** `IProfileSubsystem` (from `Core.Interfaces`).
- Do **not** import any concrete `ProfileSubsystem` type from `LobbyFeatures` in Gameplay scripts.
- Update `GameplayFeatures.asmdef` to reference `LobbyFeatures` GUID (required for `DeckButton` UI component). `DeckSummaryData` is in `Core.Interfaces` (autoReferenced — no explicit GUID needed). `IDeckSubsystem` is **never injected** into Gameplay; deck loading uses `IGameplayDeckSubsystem` which calls the API itself. When Profile moves to Core later, the `LobbyFeatures` GUID reference becomes optional.

### 6.4 Entry point (testable from Lobby)

**Same Lobby button as Track A**: "Battle". The UI must render whatever state the subsystem reports. While Track A is still under construction, Track B works against a **stub `MockSubsystemBootstrap`** that lives in `Features/Gameplay/Scripts/UI/Mock/` and only registers itself when no real subsystem is bound. It fires fake events: synthetic `PhaseChanged`, hand arrays, mock turn order. Wire it under `#if UNITY_EDITOR` via an alternate `GameplayInstaller.Editor.cs` partial.

### 6.5 Verification steps

1. **Scene load:** From Lobby Battle, Gameplay scene loads, HUD visible, both `Profile_Player` and `Profile_Enemy1` populated from `IProfileSubsystem` events. `Profile_Enemy2` hidden.
2. **Phase indicator:** Mock or real `PhaseChanged` fires → `PhaseNameValueText` updates "START PHASE" → "MAIN PHASE" etc.
3. **DeckChoose overlay:** Click `DeckButton` → `Overlay_Gameplay_Decks` appears with 8 deck slots populated from `IGameplayDeckSubsystem.DecksChanged` (fetched directly from `/api/decks` — independent of Lobby's `IDeckSubsystem`). Click slot → name+id propagate back to `PhaseInteractionPanel_DeckChoose`'s `DeckButton`. Click Confirm → panel hides on `IsReadyChanged(true)`.
4. **Hand drawer:** `Toggle_Sidebar` on `HandPanelAnchor` slides the hand panel open via `PanelDrawer` DOTween. Cards visible per `HandChanged` (filtered by local player). Panel is shown in both MainPhase (drag source for Fusion) and DrawPhase (card management) — verify `PanelVisibilityRouter` has `HandPanelAnchor` entries for both phases.
5. **Fusion flow:** Drag from `HandPanel.CardSlot` → `FusionPanel.FuseSlot1`. Calls `IFusionSubsystem.StageEquipSpell(0, cardId)`. `StagingChanged` event re-renders. Confirm → panel closes.
6. **Targeting overlay:** Click a skill in `SkillPanel` → board tiles in range highlight yellow; valid (per `target_condition`) turn green on hover; invalid red. Click → confirmation → highlights clear.
7. **TurnOrder:** Mock `QueueChanged` with 5 units → `Content` scroll view populated with 5 `CardSlot_Empty` items.
8. **DrawPhase:** Mock 2 new cards → `DrawPhasePanel` shows 2 + current hand. `HandPanelAnchor` is also visible alongside `PhaseInteractionPanel_DrawCard` (both shown simultaneously by `PanelVisibilityRouter`). Drag to discard zone. Confirm → kept cards reported via `RequestKeepCards`.
9. **MatchResult:** Mock `MatchEnded` event with `Winner=PlayerRef.Local` → crown visible on Player0 slot, Gold/XP/Time populated. Confirm → `ReturnToLobby`.

---

## 7. Hardcoded Values Reference (from LEGACY)

These values are **mined from LEGACY for reuse**, not the LEGACY code itself. They live in the new implementations as `[SerializeField]` defaults or `const` fields.

### 7.1 Board generation (use in `BoardController`)

| Constant | Value | Source |
|---|---|---|
| Row range | r ∈ [-4, 4] (9 rows) | `NetworkSpawner.GenerateBoard()` |
| Columns per row | numCols = 9 − \|r\| (5,6,7,8,9,8,7,6,5 = 61 tiles) | same |
| Axial calc | `p = -r; q = c − 4 + max(0, r);` | same |
| Tile rotation | `Quaternion.Euler(270f, 330f, 0f)` | same |
| Horizontal spacing | 1.732f (or 2·inradius from tile Z-bounds) | same |
| Vertical spacing | 1.5f (or √3·inradius from tile Z-bounds) | same |
| World→hex match threshold | 2.0f units | `BoardManager.ResolvePositionToCoordinate` |

### 7.2 Player spawn (use in `GameStateController.SpawnPlayers`)

| Player | Deploy hex (P, Q) | World rotation Y |
|---|---|---|
| Player 1 (index 0) | (4, −4) | 210° |
| Player 2 (index 1) | (−4, 4) | 30° |

### 7.3 Phase durations (use in `GameStateController` SerializeFields)

| Phase | Default duration |
|---|---|
| StartPhase | 30 s |
| MainPhase | 60 s |
| DrawPhase | 30 s |
| Match cap | 3600 s |

### 7.4 Hand & deck limits

| Constant | Value |
|---|---|
| Deck capacity | 40 (Champion=1, supports=20, granted=variable) |
| Hand max | 6 |
| Opening hand draw | 6 |
| DrawPhase draw | 2 |
| Discard capacity | 60 |

### 7.5 Tile-highlight colors (use in `TargetingOverlay`)

| State | RGB |
|---|---|
| Range | (1.00, 0.92, 0.016) — yellow |
| Valid target | (0.20, 0.80, 0.20) — green |
| Invalid target | (1.00, 0.20, 0.20) — red |

### 7.6 Damage / status baselines (live in behavior SOs — keep in sync with LEGACY for parity testing)

`seb_burning` 1 dmg/turn · `seb_melting` 2 dmg/turn · `seb_barkskin_ward` full-block next damage (consumed on trigger) · `Seedling` HP=40/Speed=2/Range=2 · `AshSoldier` HP=30/Speed=3/Range=3. Full list in LEGACY `GenericSkillBehaviorSO.cs` skill-by-skill. Skill cooldowns authoritative in GDS; see `Manual/wiring-F7.md` for per-skill values.

### 7.7 Champion HP default

100 is a dev fallback in `GameplayDeckChooseController` for the no-selection code path only. Actual champion HP comes from card data (`champion.hp` — all current champions are 20). `PlayerRosterController.SetupForMatch(championId)` must read the real value from `ICardLoadingManagerSubsystem`.

### 7.8 Move and Normal Attack base skill parameters (use in `GenericSkillBehaviorSO` for `base_move` / `base_normal_attack`)

| Skill | Skills[] index | BaseCD | TargetMask | Range | AOE / DisplayPattern |
|---|---|---|---|---|---|
| `base_move` | [0] | 1 | `EmptyTile (4)` | `1` (adjacent hex, fixed — no per-unit stat) | Center tile only — single hex, no AOE spread |
| `base_normal_attack` | [1] | 1 | From card data | From card data | From card data |

`base_move` ignores `ignore_pathfinding` — pathfinding always applies (use `RequestMove()` routing). `base_normal_attack` routes through `RequestNormalAttack()` and enters the standard 3-pass damage pipeline.

---

## 8. Coordination Plan

### Day 0 (both members, blocking) — ✅ COMPLETE

1. ✅ Land all interface files from §3 to `Core/Scripts/Interfaces/Features/Gameplay/`. Empty bodies; just contracts.
2. ✅ Land all `*StateData` structs (plain C# structs — Fusion `[Networked]` attributes stay on `NetworkBehaviour` props, not on the state structs passed through subsystem interfaces).
3. ✅ Land empty `*Subsystem` skeletons that compile (events declared, real delegation where trivial).
4. ✅ Land `GameplayInstaller` bindings for every domain (compile passes clean).
5. ✅ DeckChoose interfaces already in `Core/Scripts/Interfaces/Features/Gameplay/StartPhase/` from prior work.

**LEGACY cleanup note**: `LEGACY/GameState/GameplayPhase.cs` was deleted — it duplicated the enum now canonical in `Core.Interfaces`. Any other LEGACY files that define types already moved to `Core.Interfaces` must be deleted before they cause CS0436 / CS0433 conflicts.

This is ~1 day of work and unblocks parallelism for the next 2 weeks.

### Day 1–N
- **Member A**: works through Track A list (§5.1) in groups F1 → F7. Tests via Editor 2-instance host/client + console logs.
- **Member B**: works through Track B list (§6.1) in same group order (F1 first). Tests via mock subsystem until Track A's matching feature lands, then live.

### Integration milestone
At end of F1 (foundation), both members run a smoke test: Lobby → Gameplay scene loads, HUD visible, phase machine cycles. From here on the integration risk is low because every Subsystem contract is frozen.

---

## 9. Out of Scope (deferred follow-ups)

- AI opponent (clarified: human-vs-human only).
- Profile subsystem physical relocation to Core (logical injection already abstracted; physical move handled separately).
- `PhaseInteractionPanel_ChooseAChampion.prefab` (Champion is inside deck per rulebook §3 — leave the prefab unwired).
- 3-player matches (`Profile_Enemy2` hidden but reserved).
- BackendBridge `force end match` flow (handled at infrastructure level, not gameplay rules).
- Tile color rendering polish (use placeholder material highlights for now).
- DOTween animations beyond drawer slide + fade.

---

## 10. End-to-End Verification (after both tracks merge)

1. Two **separate Editor instances** (not MPPM — see §5.4 for why MPPM breaks identity). Both log in as different users.
2. Host clicks Battle in Lobby → Gameplay scene loads on both sides.
3. Both players see their decks listed in DeckChoose overlay, pick one, confirm.
4. StartPhase counts down, opening hand of 6 deals on each side.
5. MainPhase: each player stages a fusion (Champion + 0..4 EquipSpells), confirms → unit appears on each player's Deploy Area on both clients.
6. CombatPhase: queue visible in TurnOrder panel. Each player moves + uses one skill on their turn. Damage applies; tile effects show.
7. Repeat phases until one player's HP reaches 0.
8. MatchResultPanel shows winner, gold, XP, time.
9. Confirm → both clients return to Lobby.
10. `IBackendBridgeSubsystem.ReportMatchResultAsync` fired exactly once on host.

---

## 11. Files Touched / Created Summary

### New interfaces (Core.Interfaces asmdef)
`Core/Scripts/Interfaces/Features/Gameplay/{GameState, Board, Unit, Combat, PlayerRoster, PlayerCardZone, Fusion, TileEffect, DamagePipeline, BehaviorRegistry, Targeting, MatchResult, MatchRewards, UI}/*.cs` — ~45 files.

### New runtime scripts (GameplayFeatures asmdef)
- Track A: ~44 files across §5.1 domains (PlayerRoster, MatchRewards, and Unit public/private splits add ~8 files).
- Track B: ~13 files in `Features/Gameplay/Scripts/UI/`.

### Modified
- `GameplayInstaller.cs` — add all bindings from §5.3 + Track B panel installer registrations (panels are MonoBehaviour, auto-injected via SceneContext, no installer binding needed unless they hold non-Mono dependencies).
- `GameplayFeatures.asmdef` — add `LobbyFeatures` GUID, `Fusion.Runtime`, `Zenject`, `DOTween.Runtime`.
- `Features/Lobby/Scripts/MatchMaking/...` — add Battle button → host/join + `LoadNetworkedScene("Gameplay")`.
- `Core/Scripts/Interfaces/Features/Gameplay/Board/IBoardSubsystem.cs` — added `ContainsTile(HexCoord)`.
- `Core/Scripts/Interfaces/Features/Gameplay/Targeting/TargetingRequest.cs` — `CasterUnitId` (string) → `Caster` (NetworkId); added `IgnorePathfinding`.
- `Core/Scripts/Data/GDSModels.cs` — added `ignore_pathfinding` to `SkillData`.
- `Features/Gameplay/Scripts/Board/BoardSubsystem.cs` — implemented `ContainsTile`.
- `Features/Gameplay/Scripts/Targeting/TargetingSubsystem.cs` — implemented `RefreshRangeHighlights` + `HoverTile`; added `IUnitSubsystem` + `ICardLoadingManagerSubsystem` injections.
- `Features/Gameplay/Scripts/UI/SkillPanel.cs` — range via `HexPatternResolver.GetRange`; `IgnorePathfinding` from `skillData`; `Caster = _currentActor`.

### Reference-only (don't modify)
Everything under `Features/Gameplay/Scripts/LEGACY/`. Keep as documentation of hardcoded values only.

---

## Verification Reminder

After ExitPlanMode and approval, write this entire plan body verbatim to `Assets/_Game/Plans~/gameplay-multiplayer-plan.md` so both team members can reference it from inside the repo.
