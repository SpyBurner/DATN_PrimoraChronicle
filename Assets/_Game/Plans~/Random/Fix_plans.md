# Plan: Document UI Sync Scope + Area of Interest in `Gameplay_UI_Panels_details.md`

## Context

Two gaps in the current Gameplay docs:

1. **`Gameplay_UI_Panels_details.md`** describes each panel's visual structure and field bindings, but doesn't say where each UI element's data comes from in terms of **network scope** — replicated to all clients vs. owner-only vs. client→server intent RPC vs. purely local UI state. A developer reading this file cannot tell whether a `CardSlot` should be visible to opponents, or whether a `Toggle_Sidebar` involves any network round-trip.
2. **Neither doc addresses Area of Interest (AoI).** For a card game this is a real bug, not a polish item: `PlayerCardZoneData` in `Split-execution-gameplay.md` §3.5 declares `[Networked, Capacity(6)] public NetworkArray<NetworkString<_32>> Hand`, which replicates the full hand to all clients. Opponents could read your cards. Exploration confirms the existing `PlayerCardZoneNetworkView` (and LEGACY `NetworkPlayerState`) already broadcast `Hand[]` to everyone. There is **zero AoI implementation in the codebase today** — no `RpcTargets.InputAuthority`, no `AreaOfInterest`, no `RequestVisibility`.

The fix is to extend `Gameplay_UI_Panels_details.md` with (a) a sync-scope legend, (b) per-element sync tags on every panel, and (c) an "Area of Interest Commitments" section that names what must be owner-only — and to flag the corresponding contract change in `Split-execution-gameplay.md` §3.5 so the two split tracks build against the same rules.

This plan is documentation-only: **no code edits**, no `.cs`/`.prefab` changes.

---

## Sync-scope categories (legend to add to top of panels file)

> **Server→Client direction is always `[Networked]`. RPCs only flow client→server.** This is a hard rule to keep debugging traceable by a human: every state change a client observes comes from a Fusion replication tick, never from a server-issued RPC.

| Tag | Meaning | Fusion realization |
|---|---|---|
| **ALL_CLIENTS** | State replicated to every client | `[Networked]` property on a NetworkBehaviour visible to all clients; `Render()` pushes to subsystem |
| **OWNER_ONLY** | State replicated to the owning client only (AoI restricted) | `[Networked]` property on a per-player **private** NetworkObject; host calls `Runner.SetPlayerAlwaysInterested(owner, privateObject, true)` and never grants interest to other players. No server→client RPC variants. |
| **LOCAL_INPUT_RPC** | Client-side input → server intent | `[Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]`; display does not change until server confirms via a replicated `[Networked]` write |
| **LOCAL_ONLY** | Pure local UI state, no networking | Drawer toggles, hover highlights, drag previews, staging buffers |

---

## Critical files to modify

| File | Edit |
|---|---|
| `Assets/_Game/Plans~/Gameplay_UI_Panels_details.md` | Add sync legend preamble, append `[scope]` tag to every element bullet, add per-panel "Sync model" one-liner, add new final section "Area of Interest Commitments" |
| `Assets/_Game/Plans~/Split-execution-gameplay.md` | Split §3.5 `PlayerCardZoneData` into a public networked struct + a private networked struct on a separate per-player NetworkObject; update §3.10, §5.1, §5.2, §5.3 to reflect the split; choose Fusion AoI mechanism for the private object |

---

## Detailed edits to `Gameplay_UI_Panels_details.md`

### Edit 1 — Add a "Sync scope legend" block immediately under the title

Define the 4 tags as the table above. Single short paragraph + table.

### Edit 2 — Per-panel edits (append "Sync model" one-liner, tag every existing bullet)

**# Drawer panel anchors**
- Sync model: **LOCAL_ONLY** — pure UI animation.
- A Toggle: drives DOTween animation. `[LOCAL_ONLY]`
- Open/Closed positions: layout markers. `[LOCAL_ONLY]`

**# Layout_Fullscreen_Gameplay.prefab**
- Sync model: pure subscriber. Pulls phase + match clock from `IGameStateSubsystem`; routes the three profile slots to `GameplayPlayerProfileUI`.
- `PhaseNameValueText`: `IGameStateSubsystem.PhaseChanged` → `[ALL_CLIENTS]` (Networked `Phase` on `GameStateNetworkView`)
- `MatchTimeValueText`: `IGameStateSubsystem.MatchElapsedChanged` → `[ALL_CLIENTS]`
- `Profile_Player`: local player binding → see Profile_Gameplay
- `Profile_Enemy1`: remote player binding → see Profile_Gameplay
- `Profile_Enemy2`: inactive (3-player reserved) `[LOCAL_ONLY]`

**# Profile_Gameplay.prefab**
- Sync model: per-player profile widget. Local PFP comes from `IProfileSubsystem`; opponent PFP fetched locally by UserId; HP / name / UserId / ready all sourced from the new `IPlayerRosterSubsystem` facade (one source of truth per player). `ReadyToggle` is **always non-interactable** — it is a display-only mirror of the networked ready state.
- `ReadyToggle`: `IGameStateSubsystem.PlayerReadyChanged` for the slot's `Owner` PlayerRef → `[ALL_CLIENTS]` (Networked `PlayerReady[]` on `GameStateNetworkView`). Toggle is never interactable on either local or opponent slots.
- `Panel` (PFP, own): `IProfileSubsystem.ProfileChanged` → `[LOCAL_ONLY]` (own profile cached in local subsystem)
- `Panel` (PFP, opponent): fetched via `IHttpServiceSubsystem` GET `/api/profile/{userId}` after `IPlayerRosterSubsystem.UserIdChanged` fires → `[LOCAL_ONLY]` (the UserId itself rides `[ALL_CLIENTS]` on `PlayerRosterPublicNetworkView`; the avatar bytes are fetched per-client over HTTP)
- `NameValueText`: `IPlayerRosterSubsystem.NameChanged` → `[ALL_CLIENTS]` (Networked `PlayerName` on `PlayerRosterPublicNetworkView`)
- `HPValueText`: `IPlayerRosterSubsystem.HPChanged` → `[ALL_CLIENTS]` (Networked `HP` on `PlayerRosterPublicNetworkView`)

**# Start phase — PhaseInteractionPanel_DeckChoose.prefab**
- Sync model: local player picks a deck; selection RPC'd to server, which writes both `SelectedDeckId` and `PlayerReady[i]=true` to networked state. StartPhase is mandatory — there is no dismiss path.
- `TimeValueText`: `IGameStateSubsystem.PhaseTimeRemainingChanged` → `[ALL_CLIENTS]`
- `Button_Cancel`: skip / auto-select shortcut (per original panel-details file). Invokes the same local "use last-known deck" path that timer expiry uses, then routes through `IGameplayDeckChooseSubsystem.SubmitAsync()`. `[LOCAL_INPUT_RPC]` (still a client→server confirm — there is no dismiss flow).
- `Button_Confirm`: `IGameplayDeckChooseSubsystem.SubmitAsync()` → `Rpc_ConfirmDeckSelection`; on success the subsystem internally calls `IGameStateSubsystem.RequestSetLocalReady(true)`. `[LOCAL_INPUT_RPC]`
- `DeckButton` (selected): shows current pick. Local until confirm; afterward `SelectedDeckId` is networked. `[LOCAL_ONLY]` pre-confirm; `[ALL_CLIENTS]` post-confirm

**# Overlay_Gameplay_Decks.prefab**
- Sync model: **pure local** — fetched via HTTP, never crosses Fusion.
- `DeckSlot [×8]`: populated from `IGameplayDeckSubsystem.DecksChanged` (sources from `/api/decks`). `[LOCAL_ONLY]`
- `DeckButton` (dynamic): instantiated locally. `[LOCAL_ONLY]`
- Click: hands selection back to `PhaseInteractionPanel_DeckChoose` via local event. `[LOCAL_ONLY]`
- Exit button: local dismiss. `[LOCAL_ONLY]`

**# Draw phase — PhaseInteractionPanel_DrawCard.prefab**
- Sync model: hand keep/discard is staged locally, RPC'd on confirm. **Hand contents are OWNER_ONLY.** No UI exists for hand count, deck count, or discard pile on either player or opponent, so none of those counts are synced at all.
- `Button_Confirm`: `IPlayerCardZoneSubsystem.RequestKeepCards(keep)` → server RPC; on success the subsystem also calls `IGameStateSubsystem.RequestSetLocalReady(true)`. `[LOCAL_INPUT_RPC]`
- `CardSlot [×6]`: drawn-card slots for local player. `[OWNER_ONLY]` (see §AoI — lives on the per-player private NetworkObject). Opponent never sees these slots and never receives their contents in any form.

**# Fusion phase — PhaseInteractionPanel_Fusion.prefab**
- Sync model: staging is local; confirm triggers a server-validated unit spawn whose **public** data (position, HP, owner) becomes `[ALL_CLIENTS]` while its **skill list** stays `[OWNER_ONLY]`. The Fusion phase uses the same `IGameStateSubsystem.PlayerReady[]` handshake as StartPhase — `Button_Confirm` flips the local player's ready flag and the server advances when both players are ready or the timer hits 0. `FusionStagingData` is a plain C# struct (per §3.6), intentionally not `INetworkStruct`.
- `TimeValueText`: `IGameStateSubsystem.PhaseTimeRemainingChanged` → `[ALL_CLIENTS]`
- `Button_Cancel`: `IFusionSubsystem.ClearStaging` → local. `[LOCAL_ONLY]`
- `Button_Confirm`: `IFusionSubsystem.ConfirmFusion` (server-validated spawn RPC); on success the subsystem also calls `IGameStateSubsystem.RequestSetLocalReady(true)`. `[LOCAL_INPUT_RPC]`
- `UnitSlot`: staged base card from `FusionStagingData.ChampionOrTroopId`. `[LOCAL_ONLY]`
- `NormalAttackSlot`, `MovementSlot`: derived from base card data, display-only. `[LOCAL_ONLY]`
- `FuseSlot1..4`: staged equips from `FusionStagingData.EquipSpellIds`. `[LOCAL_ONLY]`
- (Post-confirm: a `UnitPublicNetworkView` spawns visible to all clients with HP / position / owner / status icons → `[ALL_CLIENTS]`; a paired per-unit `UnitPrivateNetworkView` carries the skill list and is AoI-restricted to the owning player → `[OWNER_ONLY]`. See §AoI rule 8.)

**# Hand phase — PhaseInteractionPanel_Hand.prefab**
- Sync model: drawer-wrapped owner-only hand display. There is no opponent-side hand UI of any kind; the opponent never receives hand contents or hand counts.
- `Toggle_Sidebar`: `PanelDrawer` open/close. `[LOCAL_ONLY]`
- `CardSlot [×N]`: `IPlayerCardZoneSubsystem.OwnHandChanged` for the local player. `[OWNER_ONLY]` (see §AoI — sourced from the per-player private NetworkObject).

**# Combat phase — PhaseInteractionPanel_Skill.prefab**
- Sync model: shows the **current actor's** skill slots — but only the actor's owning client sees the skill IDs. Skills are private per unit.
- `Toggle_Sidebar`: `[LOCAL_ONLY]`
- `CardSlot_Empty [×N]`: current actor's skill list, sourced from `IUnitSubsystem.OwnUnitSkillsChanged` (fires only on the unit-owner client). `[OWNER_ONLY]` — skills live on the per-unit private NetworkObject and are not replicated to other players. When the current actor belongs to the opponent, this panel renders empty / "opponent unit" placeholder.
- Click on a skill: `ITargetingSubsystem.BeginTargeting` (local highlight) → `ICombatSubsystem.RequestSkill` on confirm. Highlight = `[LOCAL_ONLY]`; final selection = `[LOCAL_INPUT_RPC]`.

**# Combat phase — PhaseInteractionPanel_TurnOrder.prefab**
- Sync model: action queue is public. The panel is a horizontal **scrolling** list (per the panel-details file: `ScrollView_Horizontal/Viewport/Content`). `CardSlot_Empty` instances are not fixed at 5 — `TurnOrderPanel` spawns exactly one per controllable unit currently on the board, and spawns more as units enter mid-combat (subscribing to `ICombatSubsystem.QueueChanged`).
- `Toggle_Sidebar`: `[LOCAL_ONLY]`
- `Content` (RectTransform): spawn container, layout only. `[LOCAL_ONLY]`
- `Content/CardSlot_Empty [×N]`: `ICombatSubsystem.QueueChanged`. `[ALL_CLIENTS]` (Networked `ActionQueue` on `CombatNetworkView`). Each slot displays only public unit info (owner color, HP, position) — never the unit's skills.

**# Match result — PhaseInteractionPanel_MatchResult.prefab**
- Sync model: winner/duration are replicated to everyone; gold/XP are per-viewer (each player sees only their own rewards), still via `[Networked]` AoI — never via a server-issued RPC. Match-end flow: server writes `GameMatchResult` → server writes per-player rewards to each player's private NetworkObject → server `await IBackendBridgeSubsystem.ReportMatchResultAsync(...)` → server calls `Runner.Shutdown()`. The clients' `MatchResultPanel` caches its state locally before the disconnect, so the panel stays viewable after the runner is gone. `Button_Confirm` triggers a local scene load back to Lobby — independently per client.
- `Player0/1/2 Crown`: `Image` enabled iff `GameMatchResult.Winner == thisSlot.PlayerRef`. Losing players' crowns have their `Image` component disabled. Driver = `[ALL_CLIENTS]` Networked `Winner`.
- `Player0/1/2 PFP`: fetched locally by UserId (same pattern as `Profile_Gameplay` opponent — UserId comes from `IPlayerRosterSubsystem`). `[LOCAL_ONLY]`
- `Player0/1/2 Name`: each player's `PlayerName` from `IPlayerRosterSubsystem`. `[ALL_CLIENTS]`
- `GoldValueText`: per-viewer reward. `[OWNER_ONLY]` — written by the server to the local player's `MatchRewardsPrivateNetworkView` (one per player, AoI-restricted to its owner). Snapshot is cached locally by `MatchResultPanel` on first arrival so it survives runner shutdown.
- `XPValueText`: per-viewer reward. `[OWNER_ONLY]` (same NetworkObject as GoldValueText).
- `TimeValueText`: `GameMatchResult.DurationSeconds`. `[ALL_CLIENTS]`
- `Button_Confirm`: `IMatchResultSubsystem.ReturnToLobby` → local `ISceneLoaderSubsystem.LoadScene("Lobby")`. `[LOCAL_ONLY]`. Works even after the runner has shut down — uses only the cached `GameMatchResult` snapshot, no live network calls.

### Edit 3 — Append new final section: **"Area of Interest Commitments"**

A short rules block + a table mapping each piece of state to a category. The single mechanism for OWNER_ONLY data is **Fusion's AreaOfInterest API** — never a server→client RPC. RPCs only flow client→server. Key commitments:

1. **Hand cards = OWNER_ONLY**, enforced via Fusion's AreaOfInterest API. Implementation pattern (must be reflected in `Split-execution-gameplay.md` §3.5 — see Edit 5):
   - Public per-player profile state (HP, PlayerName, UserId) lives on `PlayerRosterPublicNetworkView`, one per player, always replicated to all clients. This is consumed through the new `IPlayerRosterSubsystem` facade.
   - Private per-player card state (Hand[]) lives on a **separate** `PlayerCardZonePrivateNetworkView` NetworkObject, one per player. The host calls `Runner.SetPlayerAlwaysInterested(ownerPlayer, privateObject, true)` and **does not** add other players to its interest set. Fusion replicates the `[Networked]` Hand array only to the owning client.
   - Reshuffle from discard, draw, and discard mutations all happen on the StateAuthority and propagate to the owner client via Fusion's normal replication path — but only the owner receives them because the private NetworkObject is outside other clients' AoI.
   - **AoI mode requirement**: Fusion's `SetPlayerAlwaysInterested` requires the Runner to be in a topology that supports it (Host mode with AoI enabled, or Shared mode). The plan states this requirement up front so it's wired during scene/runner setup, not discovered late.
2. **Deck contents = SERVER_ONLY.** Never replicated to any client. There is no Deck UI on either side, so no `DeckCount` is networked either.
3. **Discard pile contents = SERVER_ONLY.** There is no Discard UI on either side, so neither pile contents nor `DiscardCount` are networked.
4. **Fusion staging = LOCAL_ONLY.** `FusionStagingData` is plain C# (§3.6) — keep it that way. Only the post-confirm spawn is networked.
5. **Targeting highlights = LOCAL_ONLY.** Yellow range / green valid / red invalid run locally on the targeting client. Only the final `HexCoord` is `LOCAL_INPUT_RPC`.
6. **Match rewards (Gold/XP) = OWNER_ONLY** per viewer, via AoI on a per-player `MatchRewardsPrivateNetworkView` (one per player). Server writes the rewards as `[Networked]` props; only the owner sees them. **No TargetedRpc** — server→client is always `[Networked]`. The owning client caches the snapshot in `MatchResultPanel` so the panel survives the post-match runner shutdown.
7. **DeckChoose `SelectedDeckId` = ALL_CLIENTS** by current design (`GameplayDeckChooseNetworkView` already exposes it as a `[Networked]` property). Acceptable for v1; could be downgraded to OWNER_ONLY in a follow-up if deck-picking should be blind.
8. **Unit skills = OWNER_ONLY**, via AoI on a per-unit `UnitPrivateNetworkView`. Public unit data (position, HP, owner, status icons) lives on `UnitPublicNetworkView` → ALL_CLIENTS. Skill IDs / cooldowns / one-time flags live on `UnitPrivateNetworkView` → only the unit-owner's client.
9. **Phase, timers, board layout, tile effects, action queue, current actor, public unit data (HP / position / owner), public player data (HP / name / UserId), ready states = ALL_CLIENTS.** Default for everything else.

### Edit 4 — Cross-reference note

At the bottom of the panels file, add a one-line callout:

> Hand visibility, unit-skill visibility, and per-player rewards are enforced via Fusion AreaOfInterest with per-player / per-unit private NetworkObjects — see `Split-execution-gameplay.md` §3.5 (PlayerRoster + PlayerCardZone split), §3.3 (Unit split), §3.9 (MatchRewards), and §5.2 (private-state prefabs).

---

## Detailed edits to `Split-execution-gameplay.md`

### Edit 5 — Rewrite §3.5 `PlayerCardZone` contract + introduce `IPlayerRosterSubsystem`

Split the original monolithic `PlayerCardZoneData` into two concerns:

1. A new **`IPlayerRosterSubsystem`** owns *all* per-player public profile data (HP, Name, UserId, derived Ready mirror). `Profile_Gameplay`, `MatchResultPanel`, and any other widget that displays per-player profile info subscribes here — a single source of truth.
2. `IPlayerCardZoneSubsystem` becomes cards-only and owner-private. No public counts (no UI for them). Deck and Discard are server-only.

```csharp
// PlayerRoster/PlayerRosterPublicData.cs  — always replicated to all clients
public struct PlayerRosterPublicData : INetworkStruct {
    public PlayerRef Owner;
    public int HP;
    public NetworkString<_32> PlayerName;
    public NetworkString<_32> UserId;          // for HTTP avatar fetch
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

// PlayerCardZone/PlayerCardZonePrivateData.cs  — replicated only to Owner via AoI
public struct PlayerCardZonePrivateData : INetworkStruct {
    public PlayerRef Owner;
    [Networked, Capacity(6)] public NetworkArray<NetworkString<_32>> Hand => default;
    // Deck and Discard are server-only — no [Networked] mirror.
}

public interface IPlayerCardZoneSubsystem : ISubsystem {
    // owner-only events — fire only on the owning client (private NetworkView is not in others' AoI)
    event UnityAction<IReadOnlyList<string>> OwnHandChanged;

    IReadOnlyList<string> GetOwnHand();   // returns local player's hand; empty for non-owners

    // Server intents (LOCAL_INPUT_RPC)
    void RequestDraw(PlayerRef p, int count);
    void RequestKeepCards(PlayerRef p, IReadOnlyList<string> keep);
    void RequestPlayMainPhaseSpell(string cardId, HexCoord target);
}
```

Callouts under the structs:

> **PlayerRoster** is a thin public facade for per-player profile data. `PlayerRosterPublicNetworkView` is one NetworkObject per player, always-replicated. HP changes drive `Profile_Gameplay.HPValueText` and `MatchResultPanel`; PlayerName drives `NameValueText` everywhere; UserId drives the local HTTP avatar fetch in both panels.

> **PlayerCardZone** is owner-private. Hand lives on `PlayerCardZonePrivateNetworkView` — a separate NetworkObject per player, made visible only to its `Owner` via `Runner.SetPlayerAlwaysInterested(owner, privateObject, true)`. Deck and Discard are server-side state inside `PlayerCardZoneModel`; they are never `[Networked]` because no UI consumes them. The subsystem only exposes `OwnHandChanged` — opponents have no hand events at all.

### Edit 6 — Update §3.10 bridges paragraph

Replace the single `IPlayerCardZoneNetworkBridge` mention with the new bridge set:

> `IPlayerRosterNetworkBridge`, `IPlayerCardZonePrivateNetworkBridge`, `IUnitPublicNetworkBridge`, `IUnitPrivateNetworkBridge`, `IMatchRewardsPrivateNetworkBridge`. The roster bridge handles HP/Name/UserId mutations. The PlayerCardZone private bridge handles hand mutations (Deck/Discard never leave the server). Unit public/private bridges split public unit fields (HP/position/owner) from owner-only fields (skills/cooldowns). Match-rewards private bridge handles per-player Gold/XP writes. Each private bridge is registered only with the StateAuthority's view of each owner's private object; AoI ensures it replicates only to the owner.

### Edit 7 — Update §5.1 domain rows

Replace the `PlayerCardZone/` row and add `PlayerRoster/` and `MatchRewards/`:

| `PlayerRoster/` | `PlayerRosterModel.cs`, `PlayerRosterController.cs`, `PlayerRosterSubsystem.cs`, `PlayerRosterPublicNetworkView.cs` |
| `PlayerCardZone/` | `PlayerCardZoneModel.cs`, `PlayerCardZoneController.cs`, `PlayerCardZoneSubsystem.cs`, `PlayerCardZonePrivateNetworkView.cs` |
| `MatchRewards/` | `MatchRewardsModel.cs`, `MatchRewardsController.cs`, `MatchRewardsSubsystem.cs`, `MatchRewardsPrivateNetworkView.cs` |

Also update the `Unit/` row to add `UnitPublicNetworkView.cs` and `UnitPrivateNetworkView.cs` (replacing the single `UnitNetworkView.cs`).

### Edit 8 — Update §5.2 prefab table

Replace the `PlayerCardZoneState.prefab` row and the `NetworkUnit.prefab` row with the split prefabs:

| `PlayerRosterPublicState.prefab` | NetworkObject | `PlayerRosterPublicNetworkView`. One per player. Always-replicated. |
| `PlayerCardZonePrivateState.prefab` | NetworkObject | `PlayerCardZonePrivateNetworkView`. One per player. Host calls `SetPlayerAlwaysInterested(owner, this, true)` on spawn; never adds other players' interest. |
| `NetworkUnitPublic.prefab` | NetworkObject | `UnitPublicNetworkView`. Public unit data (HP / position / owner / status). One per spawned unit, visible to all. |
| `NetworkUnitPrivate.prefab` | NetworkObject | `UnitPrivateNetworkView`. Owner-only unit data (skills / cooldowns / one-time flags). One per spawned unit; host calls `SetPlayerAlwaysInterested(unitOwner, this, true)` on spawn. |
| `MatchRewardsPrivateState.prefab` | NetworkObject | `MatchRewardsPrivateNetworkView`. One per player. Host writes Gold/XP at match-end; AoI restricted to the owner. Owning client caches the values locally so they survive runner shutdown. |

### Edit 9 — Add an AoI requirements bullet in §F1.1 (Scene bootstrap)

Append to F1.1 Components cell:

> Verify the `NetworkRunner` is configured for **Host mode with AreaOfInterest enabled** (or Shared mode). `SetPlayerAlwaysInterested` requires this; the call is a no-op in the default replicate-everything topology.

### Edit 10 — Add §5.5 verification steps for privacy + match-end shutdown

Append verification steps after step 5 ("Damage pipeline"):

> **5b. Hand privacy:** Two Editor instances, Host + Client. Inspect Client's Runner: `runner.IsPlayerInterested(localPlayer, hostPrivateObject)` returns `false`. Add a debug log to `PlayerCardZonePrivateNetworkView.Render()` printing the Hand contents — log fires only on the owning client, never on the opponent.
>
> **5b-2. Skill privacy:** Spawn a unit on Host. On Client, inspect that unit's `UnitPrivateNetworkView` — its Skills array is empty (no AoI interest). Open the `PhaseInteractionPanel_Skill` drawer when the host's unit is the current actor on the Client: panel renders the "opponent unit" placeholder, never the skill IDs.
>
> **5b-3. Rewards privacy:** Trigger match end with Host as winner. Inspect both clients' `MatchRewardsPrivateNetworkView` instances: each client only has access to its own. Host's Gold/XP fields are non-zero locally; Client's reflect *their* rewards, not the host's.
>
> **5b-4. Match-end shutdown survives the panel:** Trigger match end. Server log order: `ReportMatchResultAsync` completes → `Runner.Shutdown()` invoked. On both clients, after the disconnect log fires, `PhaseInteractionPanel_MatchResult` remains fully populated (winner crown, name, time, owner's Gold/XP). Clicking `Button_Confirm` triggers `SceneLoader.LoadScene("Lobby")` with no network calls.

---

## Critical files to read (during execution)

| File | Why |
|---|---|
| `Assets/_Game/Plans~/Gameplay_UI_Panels_details.md` | Target of Edits 1–4 |
| `Assets/_Game/Plans~/Split-execution-gameplay.md` §3.5, §3.10, §5.1, §5.2, §5.5, §F1.1 | Target of Edits 5–10; verify section numbers and surrounding context before patching |
| `Assets/_Game/Features/Gameplay/Scripts/DeckChoose/GameplayDeckChooseNetworkView.cs` | Confirm the `[Networked]` + `Render()` + `ChangeDetector` pattern still applies to the public/private split |
| `Assets/_Game/Features/Gameplay/Scripts/LEGACY/Network/NetworkPlayerState.cs` | Confirms current "replicate everything" pattern — contrast point in the AoI section |
| `Packages/com.exitgames.fusion/...` (Fusion runtime, exact path TBD) | Verify the exact API name — `Runner.SetPlayerAlwaysInterested(PlayerRef, NetworkObject, bool)` vs. nearby variants — and confirm Host mode supports it before recommending it in the §F1.1 bullet |

---

## Verification

This is a docs-only change, so verification is reading-driven, not runtime:

1. Open `Gameplay_UI_Panels_details.md`. Confirm the sync legend appears immediately under the title.
2. Walk every panel section. Each panel must have:
   - A `Sync model:` one-liner introducing its network role.
   - A `[ALL_CLIENTS]` / `[OWNER_ONLY]` / `[LOCAL_INPUT_RPC]` / `[LOCAL_ONLY]` tag on every existing detail bullet (no bullet untagged).
3. Confirm the new "Area of Interest Commitments" section is the final section of the panels file, contains the 9 rules, names AoI as the **sole** OWNER_ONLY mechanism (no TargetedRpc), and references `Split-execution-gameplay.md` §3.3/§3.5/§3.9/§5.2.
4. Open `Split-execution-gameplay.md`. Confirm:
   - §3.5 introduces `IPlayerRosterSubsystem` with `PlayerRosterPublicData`, and reshapes `IPlayerCardZoneSubsystem` to owner-private (Hand only; no public counts; no Deck/Discard in `[Networked]`).
   - §3.3 splits unit state into a public NetworkView (HP/position/owner) and a private NetworkView (skills/cooldowns), AoI-restricted to the unit owner.
   - §3.9 introduces a `MatchRewardsPrivateNetworkView` per player, AoI-restricted, with cached snapshot for post-shutdown viewing.
   - §3.10 lists the new bridges (`IPlayerRosterNetworkBridge`, `IPlayerCardZonePrivateNetworkBridge`, `IUnitPublicNetworkBridge`, `IUnitPrivateNetworkBridge`, `IMatchRewardsPrivateNetworkBridge`).
   - §5.1 has `PlayerRoster/`, `PlayerCardZone/` (with only the private NetworkView), `MatchRewards/`, and the updated `Unit/` rows.
   - §5.2 has rows for `PlayerRosterPublicState.prefab`, `PlayerCardZonePrivateState.prefab`, `NetworkUnitPublic.prefab`, `NetworkUnitPrivate.prefab`, `MatchRewardsPrivateState.prefab`.
   - §F1.1 (Components cell of the Scene-bootstrap row) names the Host-mode AoI requirement.
   - §5.5 has the new privacy + shutdown verification steps (5b, 5b-2, 5b-3, 5b-4).
5. Cross-check: every `[OWNER_ONLY]` tag in a panel bullet must be covered by an AoI rule **and** by the matching private-NetworkView declaration. No orphan claims. No tag may use TargetedRpc as its realization.
6. Run nothing in Unity. No `.cs` or `.prefab` changes. No `Track A`/`Track B` member starts implementing yet — those are downstream of this docs change.

---

## Decisions confirmed with user

- **Scope**: Both files get edited in one pass — `Gameplay_UI_Panels_details.md` (Edits 1–4 + 13) and `Split-execution-gameplay.md` (Edits 5–12 + 14–15).
- **AoI mechanism**: Fusion's `AreaOfInterest` API (`Runner.SetPlayerAlwaysInterested`). Implementation is via per-player / per-unit private NetworkObjects. **No TargetedRpc** — server→client direction is always `[Networked]`; RPCs flow only client→server. The execution agent must verify the exact Fusion API surface in `Packages/` before writing the §F1.1 requirement.
- **Per-player profile metadata**: lives on a new `IPlayerRosterSubsystem` facade (HP, Name, UserId), separate from `IPlayerCardZoneSubsystem` (which becomes Hand-only and owner-private).
- **Match-end shutdown**: Host writes per-player rewards via AoI → reports to BE → calls `Runner.Shutdown()` immediately. Clients cache the `MatchResult` snapshot locally so the panel survives the disconnect; `Button_Confirm` then triggers a local-only scene load to Lobby.
- **Ready toggle interactivity** (Issue 3): `ReadyToggle` is **always non-interactable** on every slot — pure display mirror of `PlayerReady[i]`. Phase confirmation happens via each phase's `Button_Confirm`, never via the toggle.
- **Ready lock policy** (Issue 4): Option B — once `PlayerReady[i]=true`, server rejects further `RequestSetLocalReady(false)` until the phase advances.
- **Setup-phase ready** (Issue 5): `AcceptsReadyInput` is explicitly **false** during `Setup` (deck not yet chosen).
- **Per-phase ready ownership** (Issue 2): `IGameStateSubsystem.PlayerReady[]` is the **only** ready flag in the system. `IGameplayDeckChooseSubsystem.IsReady` and any analogous per-subsystem ready flags are removed.

---

## Follow-up topic — Ready-state contract across phases

### Problem the user is calling out

The current docs say almost nothing about per-phase ready states beyond DeckChoose. The gap has been closed by making `IGameStateSubsystem.PlayerReady[]` the only ready signal in the system and reducing `ReadyToggle` to a non-interactable display mirror. The fix is captured in Edits 11–15 below.

### Edits to land (after Edits 1–10 above)

#### Edit 11 — Define a cross-phase Ready contract in `Split-execution-gameplay.md` §3.1

Extend `GameStateData` and `IGameStateSubsystem` so readiness is owned **solely** by the phase machine — no per-phase subsystem keeps its own ready state. `IGameplayDeckChooseSubsystem.IsReady` (and any analogous per-phase ready flags) are removed; DeckChoose owns only `SelectedDeckId`. Every phase that needs a player-confirm handshake routes through `IGameStateSubsystem.RequestSetLocalReady`.

```csharp
public struct GameStateData : INetworkStruct {
    public GameplayPhase Phase;
    public float PhaseTimeRemaining;
    public float MatchElapsed;
    public int RoundNumber;
    public PlayerRef CurrentCombatActor;
    [Networked, Capacity(4)] public NetworkArray<NetworkBool> PlayerReady => default;  // index by PlayerRef.PlayerId
}

public interface IGameStateSubsystem : ISubsystem {
    // existing events ...
    event UnityAction<PlayerRef, bool> PlayerReadyChanged;
    event UnityAction AllPlayersReady;              // fires once when every active player is ready

    bool IsReady(PlayerRef p);
    bool AcceptsReadyInput { get; }                 // true only during Start/Main/Draw; false in Setup/Combat/GameOver

    // LOCAL_INPUT_RPC — the only entry point for confirming the current phase.
    // Server validates that AcceptsReadyInput is true and that PlayerRef matches RPC source.
    // Once a player's PlayerReady[i] is true, further RequestSetLocalReady(false) calls are
    // rejected — ready is locked until the phase advances (Option B from review).
    void RequestSetLocalReady(bool ready);
}
```

Phase advancement rule (server, in `GameStateController`):
- Server advances when `AllPlayersReady` fires **or** `PhaseTimeRemaining` reaches 0.
- On phase change, server resets `PlayerReady[i] = false` for every player.
- `AcceptsReadyInput` is false during `Setup` (no decks chosen yet), `CombatPhase` (advancement is driven by `ICombatSubsystem` queue exhaustion), and `GameOver`. It is true during `StartPhase`, `MainPhase`, and `DrawPhase`.
- Once a player has set `PlayerReady[i] = true`, the server ignores subsequent un-ready RPCs from that player until the phase advances. The toggle UI displays the locked state via its non-interactable mirror (see Edit 13).

#### Edit 12 — Update §F2.4 and add §F3.x, §F5.x companions

Replace "Auto-confirm on timer expiry" (currently only F2.4) with three rows that share the same mechanism via `IGameStateSubsystem.RequestSetLocalReady`:

| # | Feature | Rule ref | Components |
|---|---|---|---|
| F2.4 | Auto-confirm Start on timer expiry | §5 Start | `GameStateController` calls `IGameplayDeckChooseSubsystem.AutoConfirmLastDeck()` for any unready player at timer 0 (commits a default deck), which then routes through the standard `SubmitAsync` → `RequestSetLocalReady(true)` path. `PlayerReady[i]` flips as a result, not directly. |
| F3.6 | Main-phase ready/confirm + auto-advance | §5 Main | Confirm button on `PhaseInteractionPanel_Fusion` and/or `PhaseInteractionPanel_Hand` calls `IGameStateSubsystem.RequestSetLocalReady(true)`. Timer-0 fallback flips remaining players. |
| F5.3 | Draw-phase ready/confirm + auto-advance | §5 Draw | Same pattern: `DrawPhasePanel.Button_Confirm` calls `RequestSetLocalReady(true)`. Cards already routed via `RequestKeepCards`; ready is the separate "I'm done" signal. |

DeckChoose keeps its own `IGameplayDeckChooseSubsystem.SubmitAsync` for the **deck-selection payload** only — but the per-subsystem `IsReady` flag (and `IsReadyChanged` event) are removed. DeckChoose's `SubmitAsync` internally calls `_gameStateSubsystem.RequestSetLocalReady(true)` after a successful `Rpc_ConfirmDeckSelection`. Any UI that previously subscribed to `IGameplayDeckChooseSubsystem.IsReadyChanged` must rebind to `IGameStateSubsystem.PlayerReadyChanged`. Same applies to Fusion (`IFusionSubsystem.ConfirmFusion`) and DrawPhase (`IPlayerCardZoneSubsystem.RequestKeepCards`): the success path of each calls `RequestSetLocalReady(true)`; they do not carry their own ready flags.

#### Edit 13 — Add a "Ready-state binding" section to `Gameplay_UI_Panels_details.md` (after Edit 3, before Edit 4 cross-reference)

Spell out that `ReadyToggle` is purely a display indicator — never interactable:

> **Ready toggle binding model.** `Profile_Gameplay.prefab` is dropped into three HUD slots (`Profile_Player`, `Profile_Enemy1`, `Profile_Enemy2`). `ReadyToggle` is a **display-only** indicator on every slot — `interactable = false` always, including the local player's slot. Clicking it does nothing.
>
> | Slot | `ReadyToggle.interactable` | Subscribes to | On click |
> |---|---|---|---|
> | `Profile_Player` (local PlayerRef) | always `false` | `IGameStateSubsystem.PlayerReadyChanged` filtered to local PlayerRef | n/a — display only |
> | `Profile_Enemy1` (remote PlayerRef) | always `false` | `IGameStateSubsystem.PlayerReadyChanged` filtered to remote PlayerRef | n/a — display only |
> | `Profile_Enemy2` | hidden by `GameplayHUDController` (3-player reserved) | n/a | n/a |
>
> The local player's ready state is set by their **phase-confirm button** (DeckChoose Confirm, Fusion Confirm, DrawPhase Confirm) — not by clicking the toggle. Each of those buttons routes through its phase-specific subsystem RPC and then calls `IGameStateSubsystem.RequestSetLocalReady(true)`; the resulting networked write flips the toggle visual on every client.
>
> `GameplayPlayerProfileUI` takes a `PlayerRef Owner` injected at bind time (set by `GameplayHUDController` when it routes each slot) and uses that purely to filter which `PlayerReadyChanged` updates concern it. There is no "local vs remote" interactivity branch — both slots are identical passive subscribers.
>
> **What this design guarantees:**
> - **Single ready entrypoint** — only phase-confirm buttons can set ready. The toggle is never a duplicate input path, so it cannot fire stale or unauthorized RPCs.
> - **Both panels look the same** — they render the same per-player networked `PlayerReady[i]`, on purpose. Neither is clickable.
> - **No drift** — both slots subscribe to the same `PlayerReadyChanged` event sourced from `[Networked] PlayerReady[]`.

Update the existing `ReadyToggle` bullet in the `Profile_Gameplay.prefab` section (added by Edit 2) so its description matches:

> `ReadyToggle`: `IGameStateSubsystem.PlayerReadyChanged` for the slot's `Owner` PlayerRef → `[ALL_CLIENTS]` (Networked `PlayerReady[]` on `GameStateNetworkView`). Always non-interactable — see "Ready toggle binding model" below.

#### Edit 14 — Update §F1.4 HUD shell

Append to the F1.4 Components cell: "`GameplayHUDController` must call `profileUI.Bind(playerRef)` on each slot so `GameplayPlayerProfileUI` knows which PlayerRef it represents (used only to filter `PlayerReadyChanged` / `HPChanged` / `NameChanged` events for the right player — there is no interactive vs display-only branch since the ready toggle is always non-interactable)."

#### Edit 15 — Update §5.5 verification

Append after the new steps 5b / 5b-2 / 5b-3 / 5b-4:

> **5c. Ready handshake via Confirm button:** Two Editor instances. Enter StartPhase. On Host, pick a deck and click `Button_Confirm` — `PlayerReady[host]` flips true on both Host and Client; both clients' `Profile_Player.ReadyToggle` and `Profile_Enemy1.ReadyToggle` visuals flip on for the host's slot. On Client, do the same — `PlayerReady[client]` flips true on both; `AllPlayersReady` fires; phase advances to MainPhase on both clients within one Render() tick.
>
> **5d. Toggle non-interactable on every slot:** During any phase, attempt to click `ReadyToggle` on local `Profile_Player` or on `Profile_Enemy1` — neither responds (interactable=false on both); no RPC fires; `PlayerReady[]` does not change.
>
> **5e. Ready locked once true (Option B):** During StartPhase, after pressing Confirm, no API exists to un-ready the player and the server rejects any synthetic `RequestSetLocalReady(false)` until the phase advances. Verify via test harness that sending the unready RPC leaves `PlayerReady[localPlayer]` at true.
>
> **5f. AcceptsReadyInput coverage:** In Setup phase, `IGameStateSubsystem.AcceptsReadyInput` returns false (any `RequestSetLocalReady(true)` is no-op'd). In CombatPhase and GameOver, same. In StartPhase / MainPhase / DrawPhase, returns true.

### Verification additions

Append to the main Verification section:

7. Confirm `Split-execution-gameplay.md` §3.1 has the extended `GameStateData` and `IGameStateSubsystem` with `PlayerReady`, `RequestSetLocalReady`, `AcceptsReadyInput`, `PlayerReadyChanged`, `AllPlayersReady`. Confirm `IGameplayDeckChooseSubsystem.IsReady` is removed.
8. Confirm §F2.4 / F3.6 / F5.3 all reference `RequestSetLocalReady` and do not introduce per-subsystem ready flags.
9. Confirm `Gameplay_UI_Panels_details.md` has the "Ready toggle binding model" section stating the toggle is always non-interactable, and the `ReadyToggle` bullet in `Profile_Gameplay.prefab` points at it.
10. Confirm §5.5 has verification steps 5c, 5d, 5e, 5f.
11. Confirm §3.5 introduces `IPlayerRosterSubsystem` (HP, Name, UserId) and that `Profile_Gameplay` + `MatchResultPanel` consume profile data through it (not through `IPlayerCardZoneSubsystem`).
12. Confirm match-rewards flow: server writes per-player Gold/XP via `MatchRewardsPrivateNetworkView` AoI before calling `Runner.Shutdown()`, and `MatchResultPanel` snapshots the values locally so it survives the disconnect.

---

## Resolved review issues

All five issues from the prior review pass have been folded into the edits above:

- **Issue 1 — Legend mechanism for OWNER_ONLY**: legend now names AoI as the sole realization (no TargetedRpc). Rule 6 of AoI Commitments rewritten to use `MatchRewardsPrivateNetworkView` AoI instead of TargetedRpc.
- **Issue 2 — Dual ready state in StartPhase**: `IGameplayDeckChooseSubsystem.IsReady` deleted; only `IGameStateSubsystem.PlayerReady[]` remains. DeckChoose's `SubmitAsync` ends with `RequestSetLocalReady(true)`.
- **Issue 3 — Toggle interactivity**: toggle is always non-interactable on every slot. Phase confirm happens via `Button_Confirm`, never the toggle.
- **Issue 4 — Un-ready policy**: Option B — ready locks until phase advances. Server rejects `RequestSetLocalReady(false)` after the player has gone ready.
- **Issue 5 — Setup-phase ready**: `AcceptsReadyInput == false` during Setup is explicit in Edit 11.