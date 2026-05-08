# Next Features Roadmap - Primora Chronicle

Priority-based execution plan to complete the core gameplay loop and persistence bridge.

## Phase 1: Initialization & Start Phase (High Priority)
1. **[D10] Start Phase Logic**:
    - Implement `StartPhaseController` for Ban/Pick.
    - Setup `BanPickPanel` UI with countdown timer and secret confirmation.
    - Synchronize Champion selection to `GameStateModel` via Photon Fusion.
2. **[D11] Board Generation & View**:
    - Finalize `BoardSubsystem` to generate the 13x13 hex grid.
    - Implement `TileView` to handle hover/select highlights and Faction colors.
    - Setup `Cinemachine` cameras for strategic top-down and unit-focus views.

## Phase 2: Card Integration & Gameplay Loop (Medium Priority)
3. **[D12] Card Data Population**:
    - **Note: In-Progress Execution**. Creating `CardSO` assets for all ideas in the Excel sheet.
    - Update `TestBE~` seed data to match Unity ScriptableObject IDs.
4. **[D13] Hand & Action Phase**:
    - Implement `HandSubsystem` to display 4 starting cards.
    - Logic for dragging cards to the `Deploy Area` with Mana cost verification.
5. **[D14] Combat System (Speed-based Cycles)**:
    - Implement `ActionQueue` in `CombatSubsystem`.
    - Create `TurnOrderUI` to show the sequence of upcoming actions.
    - Auto-attack logic using `AttackRange` and `Damage` values from `CardSO`.

## Phase 3: Polish & Atmosphere (Low Priority)
6. **[D15] Graphical Atmosphere (Gloomy/Magical)**:
    - **Reference**: `Plans~/graphical.md`.
    - Setup URP Post-Processing volume (Bloom, Vignette, Color Grading).
    - Implement Fog and Volumetric Lighting for the Gameplay scene.
7. **[D16] Audio Layer Integration**:
    - Wire `IAudioManagerSubsystem` to all UI button events in `UIRoot`.
    - Create `AtmosphereSubsystem` to manage dynamic music transitions between Main Phase and Combat Phase.
8. **[D17] Fusion Mechanics**:
    - Implement the `FusePhase` UI and logic to combine Units and Spells.
    - Visual feedback (particles) during the fusion process.

---
*Note: Phase 1 must be completed to have a testable match transition.*
