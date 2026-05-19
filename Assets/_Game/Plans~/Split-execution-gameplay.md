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
| `IDeckSubsystem` | `Core/Scripts/Interfaces/Features/Lobby/Deck/` | Provides `IReadOnlyList<DeckSummaryData>` for the DeckChoose overlay. |

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
}

public interface IGameStateSubsystem : ISubsystem {
    event UnityAction<GameplayPhase> PhaseChanged;
    event UnityAction<float> PhaseTimeRemainingChanged;
    event UnityAction<float> MatchElapsedChanged;
    event UnityAction<int> RoundNumberChanged;
    event UnityAction<PlayerRef> CurrentCombatActorChanged;

    GameplayPhase Phase { get; }
    void RegisterNetworkBridge(IGameStateNetworkBridge bridge);
    void OnAuthoritativeStateReceived(GameStateData data);
}
```

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
    HexCoord GetDeployArea(PlayerRef owner);
}
```

### 3.3 Unit

```csharp
// Unit/IUnitSubsystem.cs
public struct UnitStateData : INetworkStruct {
    public NetworkId UnitId; public PlayerRef Owner;
    public HexCoord Position; public int CurrentHP; public int MaxHP;
    public float Speed; public int DeathAnchor; public int MoveRange;
    public bool IsPersistent;
    public int GrowthStacks;
    [Networked, Capacity(8)] public NetworkArray<StatusSlot> StatusEffects => default;
    [Networked, Capacity(4)] public NetworkArray<SkillSlot> Skills => default;
}

public interface IUnitSubsystem : ISubsystem {
    event UnityAction<NetworkId> UnitSpawned;
    event UnityAction<NetworkId> UnitDied;
    event UnityAction<NetworkId, int> UnitHPChanged;
    event UnityAction<NetworkId, HexCoord> UnitMoved;
    event UnityAction<NetworkId, string, int> StatusApplied;
    event UnityAction<NetworkId, string> StatusRemoved;

    IReadOnlyList<NetworkId> AllUnits { get; }
    bool TryGet(NetworkId id, out UnitStateData data);
}
```

### 3.4 Combat (queue + turn)

```csharp
// Combat/ICombatSubsystem.cs
public interface ICombatSubsystem : ISubsystem {
    event UnityAction<IReadOnlyList<NetworkId>> QueueChanged;
    event UnityAction<NetworkId> CurrentTurnChanged;
    event UnityAction TurnEnded;

    IReadOnlyList<NetworkId> ActionQueue { get; }
    NetworkId CurrentActor { get; }

    void RequestMove(NetworkId unit, HexCoord destination);
    void RequestNormalAttack(NetworkId unit, HexCoord target);
    void RequestSkill(NetworkId unit, string skillId, HexCoord target);
    void EndTurn();
}
```

### 3.5 PlayerCardZone (deck/hand/discard)

```csharp
// PlayerCardZone/IPlayerCardZoneSubsystem.cs
public struct PlayerCardZoneData : INetworkStruct {
    public PlayerRef Owner; public int HP;
    [Networked, Capacity(6)] public NetworkArray<NetworkString<_32>> Hand => default;
    [Networked, Capacity(40)] public NetworkArray<NetworkString<_32>> Deck => default;
    [Networked, Capacity(60)] public NetworkArray<NetworkString<_32>> Discard => default;
}

public interface IPlayerCardZoneSubsystem : ISubsystem {
    event UnityAction<PlayerRef, IReadOnlyList<string>> HandChanged;
    event UnityAction<PlayerRef, int> DeckCountChanged;
    event UnityAction<PlayerRef, int> DiscardCountChanged;
    event UnityAction<PlayerRef, int> HPChanged;

    IReadOnlyList<string> GetHand(PlayerRef p);
    int GetHP(PlayerRef p);

    // Server intents
    void RequestDraw(PlayerRef p, int count);
    void RequestKeepCards(PlayerRef p, IReadOnlyList<string> keep);
    void RequestPlayMainPhaseSpell(string cardId, HexCoord target);
}
```

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

public struct TargetingRequest {
    public TargetMask Mask;
    public int Range;
    public string DisplayPattern;
    public NetworkId Caster;
}

public interface ITargetingSubsystem : ISubsystem {
    event UnityAction<TargetingRequest> TargetingStarted;
    event UnityAction<IReadOnlyList<HexCoord>> HighlightedTilesChanged;
    event UnityAction TargetingCancelled;

    void BeginTargeting(TargetingRequest req, UnityAction<HexCoord> onConfirmed);
    void Cancel();
}
```

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

```csharp
// MatchResult/IMatchResultSubsystem.cs
public struct MatchResultData : INetworkStruct {
    public PlayerRef Winner;
    public NetworkBool IsTie;
    public int GoldEarned;
    public int XPEarned;
    public float DurationSeconds;
}

public interface IMatchResultSubsystem : ISubsystem {
    event UnityAction<MatchResultData> MatchEnded;
    Task ReturnToLobby();
}
```

### 3.10 Network bridges (one per subsystem)

For every subsystem above, an `IXxxNetworkBridge` with the RPC signatures it needs. These also live in `Core/Scripts/Interfaces/Features/Gameplay/<domain>/`. Track A implements the NetworkViews; Track B never sees them.

---

## 4. Rule Decomposition — Features × Components

Each row is **independently implementable and incrementally testable**. The "Components" column lists every script that must be touched. ★ marks the entry point Test step.

### Group F1 — Foundation (must land first)

| # | Feature | Rule ref | Components |
|---|---|---|---|
| F1.1 | **Scene bootstrap from Lobby** | — | `GameplayInstaller` (bindings); Gameplay scene's `NetworkSceneManagerDefault`; in Lobby `MatchMakingSubsystem` calls `_sceneLoader.LoadNetworkedScene(_runner, "Gameplay")`. |
| F1.2 | **Hex board generation** | §1 | `BoardModel`, `BoardController`, `BoardSubsystem`, `BoardNetworkView`, `HexTile.prefab`. Generates r∈[-4,4], numCols=9-\|r\|, rotation Euler(270,330,0), spacing horizontal=1.732 / vertical=1.5 (or computed from tile Z-bounds). Deploy areas at (4,-4) and (-4,4). |
| F1.3 | **Phase machine + match timer** | §5 | `GameStateModel`, `GameStateController`, `GameStateSubsystem`, `GameStateNetworkView`. Durations: Start=30s, Main=60s, Draw=30s, MatchCap=3600s. |
| F1.4 | **HUD shell** | — | `GameplayHUDController` on `Layout_Fullscreen_Gameplay.prefab`. Wires `PhaseNameValueText`, `MatchTimeValueText`, two `Profile_*` slots. Hides `Profile_Enemy2` (3-player reserved). |
| F1.5 | **Profile bridge to HUD** | — | `GameplayPlayerProfileUI` on `Profile_Gameplay.prefab`. Inject `IProfileSubsystem` (local) + reads opponent name from `IPlayerCardZoneSubsystem` events. Maps to `NameValueText`, `HPValueText`, `Panel` (PFP), `ReadyToggle`. |

### Group F2 — Start Phase

| # | Feature | Rule ref | Components |
|---|---|---|---|
| F2.1 | **Deck selection per player** | §5 Start | Existing `GameplayDeckChoose*` stack. Finish: move 5 interface files to `Core/Scripts/Interfaces/Features/Gameplay/StartPhase/`. Wire `GameplayDeckChoosePanel` to `PhaseInteractionPanel_DeckChoose.prefab`. Wire `GameplayDeckSelectOverlay` to `Overlay_Gameplay_Decks.prefab` (8 slots, populate from `IDeckSubsystem`). |
| F2.2 | **NetworkView spawn trigger** | §5 Start | `GameStateSubsystem.OnPhaseChanged(StartPhase)` → spawns one `GameplayDeckChooseNetworkView.prefab` per player via `Runner.Spawn(prefab, inputAuthority: player)`. |
| F2.3 | **Granted-cards shuffle + opening hand** | §3 Granted, §5 Start | `PlayerCardZoneController.SetupDeckForMatch(championId, supportCardIds)`. Reads `CardLoadingManagerSubsystem.GetCardData(championId).grants_cards`, shuffles into the 20 supports, sets HP from `champion.hp`, deals 6 via `RequestDraw(6)`. |
| F2.4 | **Auto-confirm on timer expiry** | §5 Start | `GameStateSubsystem` on phase-timer 0 fires `IGameplayDeckChooseSubsystem.AutoConfirmLastDeck()` for any player whose `IsReady==false`. |

### Group F3 — Main Phase

| # | Feature | Rule ref | Components |
|---|---|---|---|
| F3.1 | **Hand panel** | §13 | `HandPanel` MonoBehaviour on `PhaseInteractionPanel_Hand.prefab`. Subscribes `IPlayerCardZoneSubsystem.HandChanged` (local player only). Renders into `CardSlot [×N]`. Drawer wrapped by `HandPanelAnchor.prefab` via `PanelDrawer`. |
| F3.2 | **Fusion staging UI** | §4 | `FusionPanel` on `PhaseInteractionPanel_Fusion.prefab`. Wires `UnitSlot`, `NormalAttackSlot` (always shown from Champion/Troop card data), `MovementSlot` (always shown), `FuseSlot1..4` (drop targets, one auto-occupied if base has `grants_skill`). `Button_Confirm` → `IFusionSubsystem.ConfirmFusion()`. |
| F3.3 | **Fusion authority** | §4 | `FusionModel`, `FusionController`, `FusionSubsystem`, `FusionNetworkView`. Validates: ≤1 unit/turn, exactly 1 base, ≤4 slots, base's `grants_skill` occupies 1 slot if present. On confirm: spawns `NetworkUnit` at deploy area, sends Troop + all EquipSpells to discard at end of Combat phase. |
| F3.4 | **MainPhaseSpell play** | §3, §5 Main | `IPlayerCardZoneSubsystem.RequestPlayMainPhaseSpell(cardId, target)`. Routes to `BehaviorRegistrySubsystem.ResolveMainPhaseSpell(behaviorId).Execute(target)`. Card moves to discard immediately. |
| F3.5 | **Champion always-available in fusion** | §3 | `FusionPanel` shows Champion card pinned to base slot pool; not consumed from hand. |

### Group F4 — Combat Phase

| # | Feature | Rule ref | Components |
|---|---|---|---|
| F4.1 | **Action queue build** | §5 Combat Step 1 | `CombatController.BuildQueue()` sorts all units by Speed desc → HP asc → coin toss. Mid-combat spawns appended via `CombatController.AppendToQueue(unitId)`. |
| F4.2 | **TurnOrder panel** | — | `TurnOrderPanel` on `PhaseInteractionPanel_TurnOrder.prefab`. Subscribes `ICombatSubsystem.QueueChanged`. Spawns card items into `Content` RectTransform. Drawer-wrapped by `TurnOrderPanelAnchor`. |
| F4.3 | **Unit turn cycle** | §5 Combat Step 2 | `CombatController.AdvanceTurn()`. On enter: tick all the actor's cooldowns by 1. Allow move + 1 action in any order. Allow skip-all. Auto-end on no-input timer. |
| F4.4 | **Movement & pathfinding** | §5 Combat | `BoardSubsystem.FindPath(from, to)` walks empty tiles only. `ignore_pathfinding: true` skips intermediate checks (destination must be empty). Max distance = unit's `MoveRange`. Knockback stops at board boundary. |
| F4.5 | **Skill panel + active skill use** | §15 | `SkillPanel` on `PhaseInteractionPanel_Skill.prefab` (drawer-wrapped). Shows current actor's 4 fusion slots as `CardSlot_Empty`. Click → calls `ITargetingSubsystem.BeginTargeting(req, onConfirm => CombatSubsystem.RequestSkill(...))`. |
| F4.6 | **Targeting display** | §9, §15 | `TargetingSubsystem` reads `display_pattern` field of skill data. `LocalInteractionController`-style highlight (yellow range, green valid, red invalid). Bitmask: Enemy=1, Ally=2, EmptyTile=4. `target_condition: 0` ⇒ self-only, no tile selection. |
| F4.7 | **3-pass damage pipeline** | §8 | `DamagePipelineSubsystem.Resolve(action)`. Aggregate → Intercept (Tile effects first then Unit effects) → Commit. Hooks: `IInterceptor` list rebuilt per action from active statuses + tile effects. |
| F4.8 | **Status effects (ScriptableObject behaviors)** | §14 | `StatusEffectBehaviorSO` base. `barkskin_ward` (intercept −15), `burning` (10/turn), `decay` (blocks heal), `rooted`, `burning_trail`, growth-related. Resolved via `status_effect_behavior_id`. |
| F4.9 | **Skill cooldowns & one-time** | §12 | `UnitController.OnTurnStart()` decrements every skill cooldown by 1. `one_time: true` skills are permanently disabled in the cycle after first use. Cooldowns reset at next StartPhase via `CombatSubsystem.OnCombatPhaseEnd`. |
| F4.10 | **Tile effects (Lingering)** | §10 | `TileEffectSubsystem`. Corrupted/Seeded/Melting; one per tile (replaces). Survives board clear (but Deploy Area force-clear wipes). Owning player's units immune to own faction's negative effects. |
| F4.11 | **Friendly-fire & faction immunity** | §2, §11 | Hardcoded checks in `DamagePipelineSubsystem.Aggregate()`: skip allied tiles unless skill has `ignore_friendly_fire: true`. |
| F4.12 | **Death & DeathAnchor** | §5 Combat Step 3 | `UnitController.OnHPZero()` immediately destroys unit, subtracts `death_anchor` from owner's `PlayerCardZone.HP`. `GameStateSubsystem.CheckElimination()` continuously (after every commit). |
| F4.13 | **Persistent units** | §6 | `is_summonable: false` units spawned via skill. Marked `IsPersistent=true` on `UnitStateData`. Survive board clear. Cooldowns persist. Deploy Area still wipes. |
| F4.14 | **Verdant evolution** | §7 | `EvolutionBehaviorSO`. At 4 Growth Stacks → swap unit identity to next form (Seedling→Sapling→Young Treant→Thorn Colossus). Stacks reset to 0. Tracked on `UnitStateData.GrowthStacks`. |
| F4.15 | **Board clear** | §5 Combat Step 4 | `CombatSubsystem.OnQueueExhausted()` when only one player's units remain (excluding persistent). All non-persistent → discard. Tile effects stay. Deploy Area force-wiped. |

### Group F5 — Draw Phase

| # | Feature | Rule ref | Components |
|---|---|---|---|
| F5.1 | **Draw 2 + hand-keep UI** | §5 Draw, §13 | `DrawPhasePanel` on `PhaseInteractionPanel_DrawCard.prefab`. Shows 2 new + current hand. Drag-and-drop keep selection. `Button_Confirm` → `IPlayerCardZoneSubsystem.RequestKeepCards(keep)`. Drops cards go to discard. Hand max=6. |
| F5.2 | **Reshuffle on empty deck** | §13 | `PlayerCardZoneController.DrawCard()`: if deck empty, shuffle Discard into Deck immediately, then draw. |

### Group F6 — Match End

| # | Feature | Rule ref | Components |
|---|---|---|---|
| F6.1 | **Win condition** | §5 Win | `GameStateSubsystem.CheckWinCondition()`: last alive wins. 1h cap → highest HP. Tie → all players Loss + penalty (flagged in `MatchResultData`). |
| F6.2 | **Match result panel** | §5 Win | `MatchResultPanel` on `PhaseInteractionPanel_MatchResult.prefab`. Wires `Player0/1/2` slots (crown, PFP, name), `GoldValueText`, `XPValueText`, `TimeValueText`. `Button_Confirm` → `IMatchResultSubsystem.ReturnToLobby()` → `ISceneLoaderSubsystem.LoadScene("Lobby")`. |
| F6.3 | **Backend report** | — | `MatchResultController.OnEnd()` calls `IBackendBridgeSubsystem.ReportMatchResultAsync(...)`. |

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
| `Unit/` | `UnitModel.cs`, `UnitController.cs`, `UnitSubsystem.cs`, `UnitNetworkView.cs` (NetworkBehaviour on every spawned unit) |
| `Combat/` | `CombatModel.cs`, `CombatController.cs`, `CombatSubsystem.cs`, `CombatNetworkView.cs` |
| `PlayerCardZone/` | `PlayerCardZoneModel.cs`, `PlayerCardZoneController.cs`, `PlayerCardZoneSubsystem.cs`, `PlayerCardZoneNetworkView.cs` (one per player) |
| `Fusion/` | `FusionModel.cs`, `FusionController.cs`, `FusionSubsystem.cs`, `FusionNetworkView.cs` |
| `TileEffect/` | `TileEffectModel.cs`, `TileEffectController.cs`, `TileEffectSubsystem.cs`, `TileEffectNetworkView.cs` |
| `DamagePipeline/` | `DamagePipelineSubsystem.cs`, `IInterceptor.cs`, `IAggregator.cs`, `IInterceptResult.cs` |
| `BehaviorRegistry/` | `BehaviorRegistrySubsystem.cs`, `BehaviorRegistryController.cs`, `BehaviorRegistryModel.cs` |
| `MatchResult/` | `MatchResultModel.cs`, `MatchResultController.cs`, `MatchResultSubsystem.cs`, `MatchResultNetworkView.cs` |
| `ScriptableObjects/` | (re-author from LEGACY) `GenericSkillBehaviorSO.cs`, `StatusEffectBehaviorSO.cs`, `MainPhaseSpellBehaviorSO.cs`, `EvolutionBehaviorSO.cs` |

### 5.2 Prefabs to create / reuse

| Prefab | Type | Notes |
|---|---|---|
| `BoardManager.prefab` | NetworkObject + GameObjectContext | Holds `BoardNetworkView`. References `IM_Tile.prefab` instance. |
| `IM_Tile.prefab` | GameObject | One instance per hex. From LEGACY (visuals reusable). |
| `GameStateManager.prefab` | NetworkObject | `GameStateNetworkView`. Spawned by host on scene start. |
| `PlayerCardZoneState.prefab` | NetworkObject | One per player. `PlayerCardZoneNetworkView`. |
| `NetworkUnit.prefab` | NetworkObject | `UnitNetworkView`. Spawned per fusion. |
| `TileEffectInstance.prefab` | NetworkObject | One per applied effect. |
| `CombatCoordinator.prefab` | NetworkObject | Singleton, `CombatNetworkView`. |
| `MatchResultCoordinator.prefab` | NetworkObject | Singleton, `MatchResultNetworkView`. |

Register every prefab in `NetworkViewRegistry` SO referenced from the scene.

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

Container.BindInterfacesAndSelfTo<PlayerCardZoneModel>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<PlayerCardZoneController>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<PlayerCardZoneSubsystem>().AsSingle().NonLazy();

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

### 5.5 Verification steps

1. **Phase loop heartbeat:** Open two Editor instances → Host + Client. Both transition `Setup → StartPhase`. Console logs from `GameStateSubsystem.PhaseChanged` fire on both sides within one Render() tick. Expand: progress through all 5 phases by clicking Confirm/auto-timeout.
2. **Board generation:** Visual check — 61 tiles in hexagonal layout (rows of 5,6,7,8,9,8,7,6,5 = 61). Deploy areas highlighted on (4,-4) and (-4,4).
3. **Unit spawn flow:** Skip to MainPhase → `IFusionSubsystem.StageBase("troop_warrior")` → ConfirmFusion → unit appears on Deploy Area for both clients.
4. **Combat queue:** Spawn 2 units (one per player) with different Speeds → CombatPhase → log shows correct order.
5. **Damage pipeline:** Use `troop_warrior` with normal attack on enemy adjacent → `UnitHPChanged` fires on both clients. Verify Aggregate→Intercept→Commit log order.
6. **Tile effect persistence across cycles:** Apply `corrupted` via skill → end CombatPhase → tile effect remains visible on `TileEffectInstance` prefab in next MainPhase.
7. **Death + DeathAnchor:** Kill a unit with `death_anchor=5` → owning player's HP drops by 5 (visible on `Profile_Player.HPValueText`).
8. **Match end:** Reduce one player's HP to 0 → `MatchResultData.Winner` fires → `ReturnToLobby` after Confirm.

---

## 6. Track B — Player UX

### 6.1 Files to create

All under `Features/Gameplay/Scripts/UI/`. Interfaces under `Core/Scripts/Interfaces/Features/Gameplay/UI/` (for the panel-side state structs only — most events come from Track A's subsystems).

| File | Prefab it lives on | Subscribes to |
|---|---|---|
| `GameplayHUDController.cs` | `Layout_Fullscreen_Gameplay.prefab` | `IGameStateSubsystem` (PhaseChanged, MatchElapsedChanged) |
| `GameplayPlayerProfileUI.cs` | `Profile_Gameplay.prefab` | `IProfileSubsystem` (local) + `IPlayerCardZoneSubsystem.HPChanged` (own + opponent) |
| `GameplayDeckChoosePanel.cs` (finish) | `PhaseInteractionPanel_DeckChoose.prefab` | `IGameplayDeckChooseSubsystem`, `IDeckSubsystem` |
| `GameplayDeckSelectOverlay.cs` | `Overlay_Gameplay_Decks.prefab` | `IDeckSubsystem.DecksChanged` |
| `DrawPhasePanel.cs` | `PhaseInteractionPanel_DrawCard.prefab` | `IPlayerCardZoneSubsystem.HandChanged` |
| `FusionPanel.cs` | `PhaseInteractionPanel_Fusion.prefab` | `IFusionSubsystem.StagingChanged`, `IPlayerCardZoneSubsystem.HandChanged` |
| `HandPanel.cs` | `PhaseInteractionPanel_Hand.prefab` | `IPlayerCardZoneSubsystem.HandChanged` |
| `SkillPanel.cs` | `PhaseInteractionPanel_Skill.prefab` | `ICombatSubsystem.CurrentTurnChanged`, `IUnitSubsystem` |
| `TurnOrderPanel.cs` | `PhaseInteractionPanel_TurnOrder.prefab` | `ICombatSubsystem.QueueChanged` |
| `MatchResultPanel.cs` | `PhaseInteractionPanel_MatchResult.prefab` | `IMatchResultSubsystem.MatchEnded`, `IProfileSubsystem` |
| `TargetingOverlay.cs` | Spawned at runtime as overlay UI / world-space tile decorator | `ITargetingSubsystem.TargetingStarted`, `HighlightedTilesChanged` |
| `CardDragHandle.cs` | Helper component on `CardSlot` prefabs for drag-and-drop into Fusion slots | local |
| `PanelVisibilityRouter.cs` | Empty GameObject in scene | `IGameStateSubsystem.PhaseChanged` → toggle which phase panel is active |

### 6.2 Prefabs to wire (no new prefabs — only component-attach + serialized field assignment)

For each prefab listed in §1.4, drop the matching `*.cs` script onto its root and serialize-field-assign children per `Gameplay_UI_Panels_details.md`. Use the existing `Tools/Primora/Add PanelDrawers to Anchors` editor menu to (re)wire drawer toggles on Hand/Skill/TurnOrder anchors.

### 6.3 Profile-to-Core preparation (in scope for Track B, not the migration itself)

To keep Gameplay agnostic of where Profile lives:
- `GameplayPlayerProfileUI` injects **only** `IProfileSubsystem` (from `Core.Interfaces`).
- Do **not** import any concrete `ProfileSubsystem` type from `LobbyFeatures` in Gameplay scripts.
- Update `GameplayFeatures.asmdef` to reference `LobbyFeatures` GUID (required for `DeckButton` and `DeckSummaryData`). When Profile moves to Core later, this reference becomes optional.

### 6.4 Entry point (testable from Lobby)

**Same Lobby button as Track A**: "Battle". The UI must render whatever state the subsystem reports. While Track A is still under construction, Track B works against a **stub `MockSubsystemBootstrap`** that lives in `Features/Gameplay/Scripts/UI/Mock/` and only registers itself when no real subsystem is bound. It fires fake events: synthetic `PhaseChanged`, hand arrays, mock turn order. Wire it under `#if UNITY_EDITOR` via an alternate `GameplayInstaller.Editor.cs` partial.

### 6.5 Verification steps

1. **Scene load:** From Lobby Battle, Gameplay scene loads, HUD visible, both `Profile_Player` and `Profile_Enemy1` populated from `IProfileSubsystem` events. `Profile_Enemy2` hidden.
2. **Phase indicator:** Mock or real `PhaseChanged` fires → `PhaseNameValueText` updates "START PHASE" → "MAIN PHASE" etc.
3. **DeckChoose overlay:** Click `DeckButton` → `Overlay_Gameplay_Decks` appears with 8 deck slots populated from `IDeckSubsystem.DecksChanged`. Click slot → name+id propagate back to `PhaseInteractionPanel_DeckChoose`'s `DeckButton`. Click Confirm → panel hides on `IsReadyChanged(true)`.
4. **Hand drawer:** `Toggle_Sidebar` on `HandPanelAnchor` slides the hand panel open via `PanelDrawer` DOTween. Cards visible per `HandChanged`.
5. **Fusion flow:** Drag from `HandPanel.CardSlot` → `FusionPanel.FuseSlot1`. Calls `IFusionSubsystem.StageEquipSpell(0, cardId)`. `StagingChanged` event re-renders. Confirm → panel closes.
6. **Targeting overlay:** Click a skill in `SkillPanel` → board tiles in range highlight yellow; valid (per `target_condition`) turn green on hover; invalid red. Click → confirmation → highlights clear.
7. **TurnOrder:** Mock `QueueChanged` with 5 units → `Content` scroll view populated with 5 `CardSlot_Empty` items.
8. **DrawPhase:** Mock 2 new cards → `DrawPhasePanel` shows 2 + current hand. Drag to discard zone. Confirm → kept cards reported via `RequestKeepCards`.
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

`burning` 10/turn · `Melting` 20/turn · `barkskin_ward` intercepts −15 · `cooldown` default 3 · `Seedling` HP=40/Speed=2/Range=2 · `AshSoldier` HP=30/Speed=3/Range=3. Full list in LEGACY `GenericSkillBehaviorSO.cs` skill-by-skill.

### 7.7 Champion HP default

100 (from existing `GameplayDeckChooseController` fallback).

---

## 8. Coordination Plan

### Day 0 (both members, blocking)
1. Land all interface files from §3 to `Core/Scripts/Interfaces/Features/Gameplay/`. Empty bodies; just contracts.
2. Land all `*StateData` structs as `INetworkStruct`.
3. Land empty `*Subsystem` skeletons that compile (events declared, methods throw `NotImplementedException`).
4. Land empty `GameplayInstaller` bindings for every domain (compile must pass).
5. Move existing DeckChoose interfaces into the new location (path harmonization).

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

1. Two Editor instances. Both log in as different users.
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
`Core/Scripts/Interfaces/Features/Gameplay/{GameState, Board, Unit, Combat, PlayerCardZone, Fusion, TileEffect, DamagePipeline, BehaviorRegistry, Targeting, MatchResult, UI}/*.cs` — ~40 files.

### New runtime scripts (GameplayFeatures asmdef)
- Track A: ~36 files across §5.1 domains.
- Track B: ~13 files in `Features/Gameplay/Scripts/UI/`.

### Modified
- `GameplayInstaller.cs` — add all bindings from §5.3 + Track B panel installer registrations (panels are MonoBehaviour, auto-injected via SceneContext, no installer binding needed unless they hold non-Mono dependencies).
- `GameplayFeatures.asmdef` — add `LobbyFeatures` GUID, `Fusion.Runtime`, `Zenject`, `DOTween.Runtime`.
- `Features/Lobby/Scripts/MatchMaking/...` — add Battle button → host/join + `LoadNetworkedScene("Gameplay")`.

### Reference-only (don't modify)
Everything under `Features/Gameplay/Scripts/LEGACY/`. Keep as documentation of hardcoded values only.

---

## Verification Reminder

After ExitPlanMode and approval, write this entire plan body verbatim to `Assets/_Game/Plans~/gameplay-multiplayer-plan.md` so both team members can reference it from inside the repo.
