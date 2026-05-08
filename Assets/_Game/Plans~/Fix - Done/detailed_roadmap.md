# Detailed Implementation Roadmap: Primora Chronicle

This roadmap breaks down the remaining technical tasks required to complete the project as specified in the Graduation Report (DACN_HK251). It follows the MVVM-Subsystem architecture and prioritizes critical path gameplay loops.

## Phase 1: Initialization & Start Phase
**Goal**: Transition from Matchmaking to a fully initialized game state with Champion selection.

### 1.1 Start Phase (Ban/Pick)
- [ ] **Data Setup**: Define `ChampionRegistrySO` to list all available champions.
- [ ] **Network State**: Add `PlayerChampionSelection` (NetworkArray) to `GameStateModel`.
- [ ] **Logic**:
    - Implement `StartPhaseController` to manage the 3-Champion selection.
    - Synchronize "Ready" states between players via RPC or Networked properties.
- [ ] **UI**: Create `ChampionPickPanel` with selection slots and countdown timer.

### 1.2 Board Initialization
- [ ] **Grid Generation**: Implement `HexGridGenerator` for the 13x13 board.
- [ ] **Tile Metadata**: Assign "Region" (Hollow/Ashen/Neutral) to tiles.
- [ ] **Camera**: Setup `Cinemachine` virtual cameras for Player 1 and Player 2 perspectives.

---

## Phase 2: The Core Loop (Turn-based Gameplay)
**Goal**: Implement the 5-phase cycle and Unit interaction logic.

### 2.1 Draw Phase
- [ ] **Deck Logic**: Implement `DeckSubsystem` logic to draw 2 cards per turn.
- [ ] **Hand Management**: Update `HandSubsystem` to handle card layout and selection.

### 2.2 Preparation (Deployment) Phase
- [ ] **Deployment Constraints**: Enforce the 5x13 "Deployment Area" for each player.
- [ ] **Unit Placement**: Implement drag-and-drop from Hand to Board with `Preview` visuals.
- [ ] **Spell Attachment**: Allow spells to be attached to units on the board (Pre-Fusion).

### 2.3 Combat Phase (Auto-Battle)
- [ ] **Speed-based Turn Order**: 
    - Calculate a sorted list of all active units by `Speed`.
    - Implement the "Action Bar" UI.
- [ ] **Auto-Attack Logic**: 
    - Search for enemies within `Attack Range`.
    - Apply damage (Atk - Def) and update HP.
- [ ] **VFX/Animations**: Trigger `Attack` and `Hurt` animations on Unit GameObjects.

### 2.4 Fusion & End Phase
- [ ] **Fusion UI**: Specialized UI for dragging up to 4 spells into a Unit's fusion slots.
- [ ] **Stat Calculation**: Apply additive/multiplicative modifiers from fused cards.

---

## Phase 3: Persistence & Economy
**Goal**: Bridge the match results to the player's account.

### 3.1 Post-Match Rewards
- [ ] **Match Result Calculation**: Determine Gold/XP based on performance (Win/Loss, duration).
- [ ] **Backend Sync**: POST results to `/api/match/complete` to persist progress.

### 3.2 Collection & Shop
- [ ] **Collection View**: Detailed filtering of owned cards.
- [ ] **Shop Logic**: Implement "Pack Opening" logic with rarity weightings.

---

## Phase 4: Polish & Integration
- [ ] **Audio System**: Wired SFX for UI and Gameplay events.
- [ ] **Tutorial System**: Sequence of guided UI popups and forced actions.
- [ ] **Optimization**: Address memory leaks in observable subscriptions.

---

## Current Progress Status
- [x] **Matchmaking Flow**: Search, Cancel, and Confirmation (Accept/Reject) implemented.
- [x] **MVVM Infrastructure**: Base classes and UIManager Subsystem established.
- [x] **Networking**: Photon Fusion integration for GameState started.
