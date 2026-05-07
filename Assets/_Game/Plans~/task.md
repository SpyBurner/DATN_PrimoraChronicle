# Project Refactoring & Feature Implementation Task List

## 0. Generation & Setup
- `[x]` Create `Plans/Changelog/changelog.md`.
- `[x]` Generate this `task.md`.
- `[x]` Refactor `UIManager` (UIRoot in Model + Layer-based cleanup).

## 1. Phase A: Foundation & Core Fixes (P1)
- `[x]` **BUG-2**: Move UI navigation logic from Controllers to Panels (Login, Register, LobbyMain).
- `[x]` **BUG-4**: Normalize `UIManager` API (Rename `ShowScreen`, `ShowPopup` to `Show<T>`).
- `[x]` **BUG-8**: Fix SceneLoader race condition. Move `ShowDefaultScreen` responsibility to `UIRoot`/`UIManager`.
- `[x]` Standardize all Controller interfaces to extend `IController` (which includes `IInitializable` and `IDisposable`).

## 2. Phase B: Global Subsystems (P1/P2)
- `[x]` Verify `HttpService` (Observable state, API abstraction).
- `[x]` Verify `AuthSession` (Cross-scene token persistence).
- `[x]` Verify `AudioManager` (Observable volumes, immediate effect).
- `[x]` Update `CoreInstaller` with any missing bindings.

## 3. Phase C: Account Scene Refactor (P2)
- `[x]` Refactor `AccountLogin` (Observable model, HttpService integration).
- `[x]` Refactor `AccountRegister`:
    - `[x]` Observable model (`Email`, `Password`, `ConfirmPassword`, `ErrorMessage`, `IsSubmitting`).
    - `[x]` Controller: Validation + API call via `HttpService`.
    - `[x]` View: Listen to events, move navigation to `AccountRegisterPanel`.

## 4. Phase D: Lobby Scene Refactor (P2)
- `[x]` Refactor `LobbyMain`:
    - `[x]` Controller: Remove navigation methods.
    - `[x]` View: Direct `UIManager` calls for screen transitions.
- `[x]` Expand `ProfileSubsystem` (Observable model + API sync).
- `[x]` Expand `BattleSubsystem` (BattleSetup state).
- `[x]` Create/Expand `MatchMakingSubsystem` (Photon/API integration).
- `[x]` Expand `DeckSubsystem` (DeckList summary).
- `[x]` **DeckBuild Subsystem**:
    - `[x]` Observable refactor + API Detail integration.
    - `[x]` Move from SceneContext to **GameObjectContext** (with `DeckBuildInstaller`).
- `[x]` Expand `ShopSubsystem` (Daily deals, card purchase).
- `[x]` Expand `SettingSubsystem` (Observable sliders + PlayerPrefs).
- `[x]` Expand `MatchHistorySubsystem` (Match list + replay loading).
- `[ ]` **CardDetail Subsystem**:
    - `[x]` Create 7 files + GameObjectContext.
    - `[ ]` Implement skill/pattern observation.

## 5. Phase E: Gameplay Scene (P3)
- `[ ]` Create `GameplayInstaller`.
- `[ ]` Implement `GameState` (Match orchestrator).
- `[x]` Implement `Hand` (Card draw/play logic with Fusion).
- `[ ]` Implement `FusePhase` (Unit/Modifier fusion).
- `[ ]` Implement `Board` (Hex grid + Unit occupancy).
- `[ ]` Implement `Combat` (Turn order + Skill execution).
- `[ ]` Implement `DrawPhase` and `MatchResult`.

## 6. Phase F: Photon Fusion Integration (P3)
- `[ ]` Networked properties for Gameplay Models.
- `[ ]` `ChangeDetector` → Observable event bridge.
- `[ ]` State Authority enforcement.

## 7. Phase G: Final Polish & Verification
- `[x]` Unity Editor Wiring Checklist (Created `wiring_plan_complete.md` and executed via MCP).
- `[ ]` Zenject Validation.
- `[ ]` Full Flow Test: Bootstrap -> Login -> Lobby -> Deck -> Battle -> Gameplay.
