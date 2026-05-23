# Gameplay Audit and Synchronization

## Objective
Audit the gameplay implementation plans (`primora-rulebook.md`, `Split-execution-gameplay.md`, and `wiring.md`) to ensure consistency, verify and fix any identified discrepancies in the codebase, and perform a manual flow check of the system from player load through the deck selection panel.

## Discrepancies Found & Fixed
1. **HP Initialization & Synchronization Authority:**
   - **Inconsistency:** `Split-execution-gameplay.md` F2.3 originally stated that HP initialization was a separate call to `PlayerRosterController.SetupForMatch(championId)`, and that player HP belonged solely to `IPlayerRosterSubsystem`, not `PlayerCardZone`. However, `PlayerCardZoneNetworkView.cs` maintains HP and explicitly routes updates to `PlayerRosterPublicNetworkView` using RPCs, matching the behavior described in `Split-execution-gameplay.md` section 3.5.
   - **Fix:** Updated `Split-execution-gameplay.md` F2.3 to reflect the codebase reality. `PlayerCardZoneNetworkView.ServerSetupDeckForMatch` initializes the HP and explicitly forwards the value to `PlayerRosterPublicNetworkView` via `GameplayNetworkCoordinator.Instance.GetPlayerRosterView(Owner)?.SendHPChangedRpc(...)`.

2. **Legacy Fields in Codebase:**
   - **Inconsistency:** `GameplayNetworkCoordinator.cs` still contained the serialized field `_playerStatePrefab`, which `wiring.md` described as a legacy holdover that should be removed.
   - **Fix:** Removed the `_playerStatePrefab` field from `GameplayNetworkCoordinator.cs` to prevent confusion and clean up the inspector, aligning with `PlayerRosterPublicNetworkView` being the F1 replacement.

3. **Rulebook Verbiage Validation:**
   - Rulebook uses terms like `"Unit subtype"` and `"Troop"` for the logic of discarding cards during fusion (`FusionNetworkView.cs`). Validated against `GDSModels.cs` and `FusionNetworkView` to ensure the logic matches the GDS exported JSON schema (checking `type == "troop"` or `"champion"`). No changes needed.

## Manual Flow Check (Lobby → StartPhase → Deck Selection)
1. Match starts, `GameStateNetworkView` immediately enters `StartPhase` and starts the 30s countdown.
2. `GameplayNetworkCoordinator` spawns all managers and player-specific network objects (`PlayerCardZoneNetworkView`, `GameplayDeckChooseNetworkView`, `PlayerRosterPublicNetworkView`).
3. Both players' UI surfaces `GameplayDeckChoosePanel`. It loads deck setups from `/api/decks` via `IGameplayDeckChooseSubsystem`.
4. User selects a deck and clicks confirm. This triggers `GameplayDeckChooseNetworkView.Rpc_ConfirmDeckSelection` to the server.
5. The server invokes `PlayerCardZoneNetworkView.ServerSetupDeckForMatch(...)`, which reads `CardLoadingManagerSubsystem.GetCardData(championId).grants_cards`, inserts them, shuffles the deck, and executes `ServerDraw(6)` to give the opening hand.
6. `GameplayDeckChoosePanel` concurrently calls `_gameState?.RequestSetLocalReady(true)`, setting the local player ready on `GameStateNetworkView`.
7. Once both players are confirmed (or the 30s `PhaseTimer` expires triggering `AutoConfirmUnreadyPlayers`), `GameStateNetworkView` advances to `MainPhase` correctly.

**Roadblocks:** None identified. The flow handles all state transitions, handles data routing per PlayerRef exactly as outlined in the Track A/Track B system, and correctly falls back gracefully via timers if the user fails to select a deck.

## Conclusion
The Split-execution Gameplay architecture holds true. The phase machine, authoritative deck initialization flow, and PlayerCardZone HP forwarding are correctly implemented and strictly separated by Area of Interest.
