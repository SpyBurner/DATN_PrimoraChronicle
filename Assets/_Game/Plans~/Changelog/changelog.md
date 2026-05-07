# Changelog — Primora Chronicle

## [2026-05-07] — Prompt Session: Deck & UI Overhaul

### Architecture & Subsystems
- **Deck Subsystem**: Split into `Deck` (summary list) and `DeckBuild` (detailed editor).
- **MVVM Refactoring**: Converted `DeckModel` and `DeckBuildModel` to use `Observable<T>` properties.
- **P1 Compliance**: Removed all `DeckSO` ScriptableObject dependencies in favor of API-driven data (`DeckSummaryData`, `DeckDetailData`).
- **Card Resolution**: Integrated `ICardLoadingManagerSubsystem` into `DeckBuildController` to resolve card IDs to `CardSO` assets.
- **UI Reactivity**: Refactored `DeckPanel` and `DeckBuildPanel` to strictly observe model events, removing polling and manual UI triggers.

### UI Infrastructure
- **Loading Screen**: Implemented `LoadingScreenPanel` (specialized `UIPanel`).
- **UIManager Transition**: Implemented `FadeIn()` and `FadeOut()` logic in `UIManagerController` to show/hide the loading screen automatically during scene loads.
- **Wiring Logic**: Updated `wiring_plan_complete.md` with instructions for the `SYSTEM` layer and loading screen setup.

### [2026-05-07] - Batch 3: Core Fixes & API Normalization
- **UIManager API**: Renamed `ShowScreen/ShowPopup` to `Show<T>` and `CloseView` to `Close`.
- **BUG-8 Fix**: Moved `ShowDefaultScreen` responsibility to `UIRoot`/`UIManager` to fix SceneLoader race conditions.
- **HttpService**: Improved error logging to include response body for better debugging of FastAPI 422 errors.
- **Account API**: Fixed payload key mismatch (`email` -> `username`) and updated response models to match nested JSON.

### [2026-05-07] - Batch 4: Lobby Expansion & Gameplay Scaffolding
- **Scoped Subsystems**: Moved `DeckBuild` and `CardDetail` to `GameObjectContext` with dedicated installers.
- **BattleSetup**: Refactored `Battle` subsystem to `BattleSetup` to better reflect its role in the Lobby.
- **CardDetail**: Implemented Skill/Pattern observation.
- **Gameplay (Hand)**: Implemented `HandModel` using Fusion `Networked` properties with `ChangeDetector` bridging to `UnityObservables`.

### Backend (TestBE)
- **New Endpoints**: Added `/api/decks` (summary), `/api/decks/{deckId}` (details), and `/api/decks/save` (mutation).
- **Schemas**: Implemented Pydantic models for `DeckSummary`, `DeckDetail`, and `DeckSaveRequest`.

### Housekeeping
- **Zenject**: Cleaned up `LobbyInstaller.cs` (removed redundant `DeckEdit` bindings).
- **Documentation**: Compiled `wiring_plan_complete.md` in `Plans~/Handwire`.
- **Bug Fixes**: Moved `RegisterPanel` to `Awake()` in `UIPanel.cs` (as per BUG-7).
