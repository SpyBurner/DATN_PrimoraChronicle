# Next Features Plan - Primora Chronicle

This plan outlines the steps to align the current project with the Graduation Report requirements, in chronological order.

## Phase 1: Matchmaking & Initialization (Immediate Priority)
1. **[UC-M03] Match Confirmation Flow**:
    - Update `MatchMakingController` to wait for a "Match Found" event instead of simulating 3s wait.
    - Implement `ConfirmationPopup` for Accept/Reject.
    - Add penalty logic for Rejection/Timeout if required.
2. **[UC-G01/02] Ban & Pick Phases**:
    - Implement `StartPhaseSubsystem` (or expand `GameStateSubsystem`).
    - Create `ChampionPickPanel` and `DeckSelectPanel`.
    - Implement secret picking and simultaneous reveal logic via Photon Fusion.

## Phase 2: Core Gameplay Loop Refinement
3. **Hex Grid Logic & Deploy Area**:
    - Finalize `BoardSubsystem` to strictly enforce `Deploy Area` per player.
    - Implement `HexCoord` distance and pattern validation for Move/Attack.
4. **[UC-G06] Combat Cycle (Speed-based)**:
    - Implement turn order calculation in `CombatSubsystem` based on Unit `Speed`.
    - Add UI for the Turn Order bar (`PhaseInteractionPanel_TurnOrder`).
5. **[UC-G04/05] Fusion & Spell Mechanics**:
    - Implement `FusePhaseController` logic for combining Unit + up to 4 Spells.
    - Define `StatModifier` system to handle HP/Attack/Speed boosts from fused spells.

## Phase 3: Economy & Persistence
6. **[UC-G08] Post-Match Rewards**:
    - Implement Reward calculation logic in `MatchResultSubsystem`.
    - Sync Gold/XP updates to the backend via `HttpService`.
7. **[UC-S01] Shop Expansion**:
    - Add level-gating to specific cards in the `ShopSubsystem`.
    - Implement "Card Pack" opening animation/logic.

## Phase 4: Advanced Features (Polish)
8. **[UC-P03] Match Replay System**:
    - Implement `ActionRecorder` in `Gameplay` to save all networked commands.
    - Create `MatchReplaySubsystem` in Lobby to parse records and re-simulate.
    - Add Play/Pause/Fast-Forward controls to the UI.
9. **[UC-D04] Card Lore & Art**:
    - Expand `CardDetailSubsystem` to include a high-resolution art mode and scrolling lore text.
10. **Audio & VFX**:
    - Integrate `AudioManagerSubsystem` with all UI buttons and gameplay actions (Attack, Fuse, Death).
    - Add VFX for Faction-specific actions (Hollow sacrifice, Ashen explosion).

---
*Note: Each step requires both Model/Controller logic and corresponding UI wiring in the Unity Editor.*
