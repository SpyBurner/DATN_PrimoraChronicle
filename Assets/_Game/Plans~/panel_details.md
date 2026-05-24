Gameplay UI Panel contents for manual wiring - script reference

## Sync scope legend

> **Server→Client direction is always `[Networked]`. RPCs only flow client→server.** Every state change a client observes comes from a Fusion replication tick, never from a server-issued RPC.

| Tag | Meaning | Fusion realization |
|---|---|---|
| **ALL_CLIENTS** | State replicated to every client | `[Networked]` property on a NetworkBehaviour visible to all clients; `Render()` pushes to subsystem |
| **OWNER_ONLY** | State replicated to the owning client only (AoI restricted) | `[Networked]` property on a per-player/per-unit **private** NetworkObject; host calls `Runner.SetPlayerAlwaysInterested(owner, privateObject, true)` and never grants interest to other players. No server→client RPC variants. |
| **LOCAL_INPUT_RPC** | Client-side input → server intent | `[Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]`; display does not change until server confirms via a replicated `[Networked]` write |
| **LOCAL_ONLY** | Pure local UI state, no networking | Drawer toggles, hover highlights, drag previews, staging buffers |

---

# Drawer panel anchors
- Script: D:\UnityProjects\DATN_PrimoraChronicle\Assets\_Game\Features\Gameplay\Scripts\UI\PanelDrawer.cs
- Panels:
    - HandPanelAnchor.prefab
    - SkillPanelAnchor.prefab
    - TurnOrderPanelAnchor.prefab
- Description: These prefabs are Drawer wrappers for the corresponding UI panel below. The UI Panel will be a child of this gameobject. The UI Panel will be moved between the Open and Closed position.
- Sync model: **LOCAL_ONLY** — pure UI animation.
- Detail:
    - A Toggle: The toggle controlling the movement of the wrapped UI Panel to move to "Open" or "Closed" position. `[LOCAL_ONLY]`
    - Open position children: a child gameobject placed at the "Open position", where the UI Panel should be when the toggle is on. `[LOCAL_ONLY]`
    - Close position: not an exclusive gameobject. The local position of 0 relative to the anchor is considered the "Closed" position. `[LOCAL_ONLY]`

# Overall layout
Layout_Fullscreen_Gameplay.prefab
- Script on root: GameplayHUDController (MonoBehaviour)
- Sync model: pure subscriber. Pulls phase + match clock from `IGameStateSubsystem`; routes the three profile slots to `GameplayPlayerProfileUI`.
- Detail:
    - PhaseNamePanel/PhaseNameValueText: TMP — current phase name ("START PHASE", "MAIN PHASE", etc.) `[ALL_CLIENTS]` (`IGameStateSubsystem.PhaseChanged` → Networked `Phase` on `GameStateNetworkView`)
    - PhaseNamePanel/MatchTimeValueText: TMP — match elapsed time `[ALL_CLIENTS]` (`IGameStateSubsystem.MatchElapsedChanged`)
    - Profile_Enemy1: GameplayPlayerProfileUI — active, top-left opponent slot `[ALL_CLIENTS]` (see Profile widget)
    - Profile_Enemy2: GameplayPlayerProfileUI — inactive by default, reserved for third player `[LOCAL_ONLY]`
    - Profile_Player: GameplayPlayerProfileUI — active, local player slot `[ALL_CLIENTS]` (see Profile widget)

# Profile widget
Profile_Gameplay.prefab
- Script on root: GameplayPlayerProfileUI (MonoBehaviour)
- Sync model: per-player profile widget. HP / name / UserId / ready are all sourced from `IPlayerRosterSubsystem` (one source of truth per player). `ReadyToggle` is **always non-interactable** — display-only mirror of the networked ready state. Local PFP comes from `IProfileSubsystem`; opponent PFP fetched locally by UserId over HTTP.
- Detail:
    - FramedContainer/ReadyToggle: Toggle — player confirmation/ready state. `[ALL_CLIENTS]` (`IGameStateSubsystem.PlayerReadyChanged` for slot's `Owner` PlayerRef → Networked `PlayerReady[]` on `GameStateNetworkView`). Always non-interactable on every slot — see "Ready toggle binding model" below.
    - FramedContainer/Panel: Image — profile picture (PFP). Own player: `IProfileSubsystem.ProfileChanged` `[LOCAL_ONLY]`. Opponent: fetched via HTTP after `IPlayerRosterSubsystem.UserIdChanged` fires `[LOCAL_ONLY]` (UserId itself is `[ALL_CLIENTS]` on `PlayerRosterPublicNetworkView`).
    - FramedContainer/Panel (1)/Panel (2)/NameValueText: TMP — player display name. `[ALL_CLIENTS]` (`IPlayerRosterSubsystem.NameChanged` → Networked `PlayerName` on `PlayerRosterPublicNetworkView`)
    - FramedContainer/Panel (1)/Panel (4)/HPValueText: TMP — current HP value. `[ALL_CLIENTS]` (`IPlayerRosterSubsystem.HPChanged` → Networked `HP` on `PlayerRosterPublicNetworkView`)

# Start phase
PhaseInteractionPanel_DeckChoose.prefab
- Description: First panel shown on match join; player picks a deck. StartPhase is mandatory — there is no dismiss path.
- Sync model: local player picks a deck; selection RPC'd to server, which writes both `SelectedDeckId` and `PlayerReady[i]=true` to networked state.
- Detail:
    - Panel/Panel/TimeValueText: TMP — countdown timer value. `[ALL_CLIENTS]` (`IGameStateSubsystem.PhaseTimeRemainingChanged`)
    - Panel/Button_Cancel: Button — skip / auto-select shortcut. Invokes the local "use last-known deck" path then routes through `IGameplayDeckChooseSubsystem.SubmitAsync()`. `[LOCAL_INPUT_RPC]`
    - Panel/Button_Confirm: Button — confirm deck selection. `IGameplayDeckChooseSubsystem.SubmitAsync()` → `Rpc_ConfirmDeckSelection`; on success internally calls `IGameStateSubsystem.RequestSetLocalReady(true)`. `[LOCAL_INPUT_RPC]`
    - Panel (1)/DeckButton: DeckButton (LobbyFeatures) — holds selected deck name + ID; queries /api/decks on enable. Local until confirm; afterward `SelectedDeckId` is networked. `[LOCAL_ONLY]` pre-confirm; `[ALL_CLIENTS]` post-confirm.

Overlay_Gameplay_Decks.prefab
- Description: Grid of 8 deck option slots.
- Sync model: **pure local** — fetched via HTTP, never crosses Fusion.
- Detail:
    - DeckSlot [×8]: The parent object to populate DeckButton objects in. `[LOCAL_ONLY]` (populated from `IGameplayDeckSubsystem.DecksChanged`, sourced from `/api/decks`)
    - DeckButton: dynamically populated, from the API call to get deck name and deck id. `[LOCAL_ONLY]`
    - On click: send the deck name and id back to PhaseInteractionPanel_DeckChoose.prefab to display, disable this panel. `[LOCAL_ONLY]`
    - Exit button: Disable this panel without making any change. `[LOCAL_ONLY]`

# Draw phase
PhaseInteractionPanel_DrawCard.prefab
- Sync model: hand keep/discard is staged locally, RPC'd on confirm. **Hand contents are OWNER_ONLY.** No UI exists for hand count, deck count, or discard pile on either side — none of those counts are synced.
- Detail:
    - Panel/Button_Confirm: Button — confirm draw. `IPlayerCardZoneSubsystem.RequestKeepCards(keep)` → server RPC; on success also calls `IGameStateSubsystem.RequestSetLocalReady(true)`. `[LOCAL_INPUT_RPC]`
    - Panel (1)/CardSlot [×6]: Button + CardDisplay — drawable card slots (CardSlot, CardSlot (1)–(5)). Drawn-card slots for local player only. `[OWNER_ONLY]` (see §AoI — lives on the per-player private NetworkObject). Opponent never sees these slots.

# Fusion phase
PhaseInteractionPanel_Fusion.prefab
- Sync model: staging is local; confirm triggers a server-validated unit spawn whose **public** data (position, HP, owner) becomes `[ALL_CLIENTS]` while its **skill list** stays `[OWNER_ONLY]`. Uses the same `IGameStateSubsystem.PlayerReady[]` handshake as StartPhase — `Button_Confirm` flips the local player's ready flag. `FusionStagingData` is a plain C# struct, intentionally not `INetworkStruct`.
- Detail:
    - Panel/Panel/TimeValueText: TMP — countdown timer. `[ALL_CLIENTS]` (`IGameStateSubsystem.PhaseTimeRemainingChanged`)
    - Panel/Button_Cancel: Button — cancel fusion. `IFusionSubsystem.ClearStaging` → local. `[LOCAL_ONLY]`
    - Panel/Button_Confirm: Button — confirm fusion. `IFusionSubsystem.ConfirmFusion` (server-validated spawn RPC); on success also calls `IGameStateSubsystem.RequestSetLocalReady(true)`. `[LOCAL_INPUT_RPC]`
    - Panel (1)/Panel/UnitSlot: Button + CardDisplay — unit being fused, staged from `FusionStagingData.ChampionOrTroopId`. `[LOCAL_ONLY]`
    - Panel (1)/Panel (1)/Panel (2)/NormalAttackSlot: Button + CardDisplay — normal attack card input, derived from base card data. `[LOCAL_ONLY]`
    - Panel (1)/Panel (1)/Panel (2)/MovementSlot: Button + CardDisplay — movement card input, derived from base card data. `[LOCAL_ONLY]`
    - Panel (1)/Panel (1)/Panel (3)/FuseSlot1: Button + CardDisplay — fusion result slot 1, staged from `FusionStagingData.EquipSpellIds`. `[LOCAL_ONLY]`
    - Panel (1)/Panel (1)/Panel (3)/FuseSlot2: Button + CardDisplay — fusion result slot 2. `[LOCAL_ONLY]`
    - Panel (1)/Panel (1)/Panel (3)/FuseSlot3: Button + CardDisplay — fusion result slot 3. `[LOCAL_ONLY]`
    - Panel (1)/Panel (1)/Panel (3)/FuseSlot4: Button + CardDisplay — fusion result slot 4. `[LOCAL_ONLY]`
    - (Post-confirm: a `UnitPublicNetworkView` spawns with HP / position / owner / status icons → `[ALL_CLIENTS]`; a paired `UnitPrivateNetworkView` carries the skill list, AoI-restricted to the owning player → `[OWNER_ONLY]`. See §AoI rule 8.)

# Hand phase
PhaseInteractionPanel_Hand.prefab
- Sync model: drawer-wrapped owner-only hand display. No opponent-side hand UI exists; the opponent never receives hand contents or counts.
- Detail:
    - Toggle_Sidebar: Toggle — drawer open/close (wired by PanelDrawer). `[LOCAL_ONLY]`
    - Panel (content area)/CardSlot [×N]: Button + CardDisplay — player hand card slots. `[OWNER_ONLY]` (`IPlayerCardZoneSubsystem.OwnHandChanged` for the local player — sourced from the per-player private NetworkObject. See §AoI rule 1.)

# Combat phase
PhaseInteractionPanel_Skill.prefab
- Sync model: shows the **current actor's** skill slots — only the actor's owning client sees the skill IDs. Skills are private per unit. When the current actor belongs to the opponent, this panel renders an "opponent unit" placeholder.
- Detail:
    - Toggle_Sidebar: Toggle — drawer open/close (wired by PanelDrawer). `[LOCAL_ONLY]`
    - Panel (content area)/CardSlot_Empty [×N]: Button + CardDisplay × 2 — unit skill slots. `[OWNER_ONLY]` (`IUnitSubsystem.OwnUnitSkillsChanged`, fires only on the unit-owner client — skills live on the per-unit private NetworkObject. See §AoI rule 8.)
    - Click on a skill: `ITargetingSubsystem.BeginTargeting` (local highlight) → `ICombatSubsystem.RequestSkill` on confirm. Highlight = `[LOCAL_ONLY]`; final selection = `[LOCAL_INPUT_RPC]`.

PhaseInteractionPanel_TurnOrder.prefab
- Sync model: action queue is public. The panel is a horizontal **scrolling** list (`ScrollView_Horizontal/Viewport/Content`). `CardSlot_Empty` instances are not fixed — `TurnOrderPanel` spawns one per controllable unit currently on the board and spawns more as units enter mid-combat.
- Detail:
    - Toggle_Sidebar: Toggle — drawer open/close (wired by PanelDrawer). `[LOCAL_ONLY]`
    - Panel/ScrollView_Horizontal/Viewport/Content: RectTransform — spawn container for turn-order card items. `[LOCAL_ONLY]` (layout only)
    - Content/CardSlot_Empty [×N]: Button + CardDisplay × 2 — unit slots, spawned dynamically per `ICombatSubsystem.QueueChanged`. `[ALL_CLIENTS]` (Networked `ActionQueue` on `CombatNetworkView`). Each slot displays only public unit info (owner, HP) — never skills.

# Match result
PhaseInteractionPanel_MatchResult.prefab
- Sync model: winner/duration replicated to everyone; Gold/XP are per-viewer via AoI — never via server RPC. Match-end flow: server writes `GameMatchResult` → server writes per-player rewards to `MatchRewardsPrivateNetworkView` → `await ReportMatchResultAsync(...)` → `Runner.Shutdown()`. `MatchResultPanel` caches all values locally before disconnect so the panel stays viewable. `Button_Confirm` is a local scene load — no network calls.
- Detail:
    - Panel/FramedContainer_Stone/Panel (1)/Panel (3)/Player0/Player0Crown: Image — winner crown. `Image` enabled iff `GameMatchResult.Winner == thisSlot.PlayerRef`; disabled on losing players. `[ALL_CLIENTS]` (Networked `Winner`)
    - Panel/FramedContainer_Stone/Panel (1)/Panel (3)/Player0/Player0PFP: Image — player 0 portrait. Fetched locally by UserId via HTTP (UserId from `IPlayerRosterSubsystem`). `[LOCAL_ONLY]`
    - Panel/FramedContainer_Stone/Panel (1)/Panel (3)/Player0/Player0Name: TMP — player 0 display name. `[ALL_CLIENTS]` (`IPlayerRosterSubsystem.NameChanged`)
    - Player1/Player1Crown, Player1PFP, Player1Name: same pattern for player 1. Crown `[ALL_CLIENTS]`; PFP `[LOCAL_ONLY]`; Name `[ALL_CLIENTS]`
    - Player2/Player2Crown, Player2PFP, Player2Name: same pattern for player 2. Crown `[ALL_CLIENTS]`; PFP `[LOCAL_ONLY]`; Name `[ALL_CLIENTS]`
    - Panel/FramedContainer_Stone/Panel/GoldValueText: TMP — gold earned. `[OWNER_ONLY]` — written by server to `MatchRewardsPrivateNetworkView` (AoI-restricted to owner); cached locally by `MatchResultPanel` on first arrival, survives runner shutdown.
    - Panel/FramedContainer_Stone/Panel/XPValueText: TMP — XP earned. `[OWNER_ONLY]` (same NetworkObject as GoldValueText).
    - Panel/FramedContainer_Stone/Panel/TimeValueText: TMP — match duration. `[ALL_CLIENTS]` (`GameMatchResult.DurationSeconds`)
    - Panel/Button_Confirm: Button — dismiss and return to lobby. `IMatchResultSubsystem.ReturnToLobby` → local `ISceneLoaderSubsystem.LoadScene("Lobby")`. `[LOCAL_ONLY]`. Works after runner shutdown — uses only the cached `GameMatchResult` snapshot.

---

## Ready toggle binding model

`Profile_Gameplay.prefab` is dropped into three HUD slots (`Profile_Player`, `Profile_Enemy1`, `Profile_Enemy2`). `ReadyToggle` is a **display-only** indicator on every slot — `interactable = false` always, including the local player's slot. Clicking it does nothing.

| Slot | `ReadyToggle.interactable` | Subscribes to | On click |
|---|---|---|---|
| `Profile_Player` (local PlayerRef) | always `false` | `IGameStateSubsystem.PlayerReadyChanged` filtered to local PlayerRef | n/a — display only |
| `Profile_Enemy1` (remote PlayerRef) | always `false` | `IGameStateSubsystem.PlayerReadyChanged` filtered to remote PlayerRef | n/a — display only |
| `Profile_Enemy2` | hidden by `GameplayHUDController` (3-player reserved) | n/a | n/a |

The local player's ready state is set by the **phase-confirm button** (DeckChoose Confirm, Fusion Confirm, DrawPhase Confirm) — not by clicking the toggle. Each button routes through its phase-specific subsystem RPC and then calls `IGameStateSubsystem.RequestSetLocalReady(true)`; the resulting networked write flips the toggle visual on every client.

`GameplayPlayerProfileUI` takes a `PlayerRef Owner` injected at bind time via `profileUI.Bind(playerRef)` (called by `GameplayHUDController` when routing each slot). It uses `Owner` only to filter which `PlayerReadyChanged` / `HPChanged` / `NameChanged` events concern it. There is no "local vs remote" interactivity branch — both slots are identical passive subscribers.

**What this design guarantees:**
- **Single ready entrypoint** — only phase-confirm buttons can set ready. The toggle cannot fire stale or unauthorized RPCs.
- **Both panels look the same** — they render the same per-player networked `PlayerReady[i]`, on purpose. Neither is clickable.
- **No drift** — both slots subscribe to the same `PlayerReadyChanged` event sourced from `[Networked] PlayerReady[]`.

---

## Area of Interest Commitments

The single mechanism for OWNER_ONLY data is **Fusion's AreaOfInterest API** (`Runner.SetPlayerAlwaysInterested`). Server→Client data always travels via `[Networked]` replication — never via a server-issued RPC.

1. **Hand cards = OWNER_ONLY**, enforced via Fusion AoI. Private per-player card state (Hand[]) lives on a separate `PlayerCardZonePrivateNetworkView` NetworkObject per player. Host calls `Runner.SetPlayerAlwaysInterested(ownerPlayer, privateObject, true)` and does not add other players' interest. Public per-player profile state (HP, PlayerName, UserId) lives on `PlayerRosterPublicNetworkView`, always-replicated, consumed through `IPlayerRosterSubsystem`. **AoI mode requirement**: Host mode with AreaOfInterest enabled (or Shared mode) — `SetPlayerAlwaysInterested` is a no-op in the default replicate-everything topology; wire this during runner setup.

2. **Deck contents = SERVER_ONLY.** Never replicated to any client. No Deck UI exists on either side, so no `DeckCount` is networked.

3. **Discard pile contents = SERVER_ONLY.** No Discard UI exists on either side, so neither pile contents nor `DiscardCount` are networked.

4. **Fusion staging = LOCAL_ONLY.** `FusionStagingData` is plain C# (§3.6) — not `INetworkStruct`. Only the post-confirm unit spawn is networked.

5. **Targeting highlights = LOCAL_ONLY.** Yellow range / green valid / red invalid run locally on the targeting client. Only the final `HexCoord` is `LOCAL_INPUT_RPC`.

6. **Match rewards (Gold/XP) = OWNER_ONLY** per viewer, via AoI on a per-player `MatchRewardsPrivateNetworkView`. Server writes rewards as `[Networked]` props; only the owner's client receives them. The owning client caches the snapshot in `MatchResultPanel` so the panel survives the post-match runner shutdown.

7. **DeckChoose `SelectedDeckId` = ALL_CLIENTS** by current design (`GameplayDeckChooseNetworkView` exposes it as a `[Networked]` property). Acceptable for v1; can be downgraded to OWNER_ONLY in a follow-up if deck-picking should be blind.

8. **Unit skills = OWNER_ONLY**, via AoI on a per-unit `UnitPrivateNetworkView`. Public unit data (position, HP, owner, status icons) lives on `UnitPublicNetworkView` → ALL_CLIENTS. Skill IDs / cooldowns / one-time flags live on `UnitPrivateNetworkView` → only the unit-owner's client.

9. **Phase, timers, board layout, tile effects, action queue, current actor, public unit data (HP / position / owner), public player data (HP / name / UserId), ready states = ALL_CLIENTS.** Default for everything not listed above.

---

> Hand visibility, unit-skill visibility, and per-player rewards are enforced via Fusion AreaOfInterest with per-player / per-unit private NetworkObjects — see `Split-execution-gameplay.md` §3.5 (PlayerRoster + PlayerCardZone split), §3.3 (Unit split), §3.9 (MatchRewards), and §5.2 (private-state prefabs).
