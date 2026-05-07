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

### [2026-05-07] - Batch 5: UI Automation & Wiring
- **Prefab Management**: Automatically attached `UIPanel` scripts to 14 UI prefabs across Account, Lobby, and System features via MCP.
- **UIMappingSO**: Programmatically mapped all UI prefabs to `UIIdentifier` enums in `UIMappingSO.asset`, ensuring correct ID order and scene defaults.
- **Scene Defaults**: Configured `DefaultUIMappings` for Bootstrap, Account, and Lobby scenes.

### [2026-05-07] - Batch 6: Networking & API integrity
- **Hand Networking**: Implemented `PlayCard` functionality in `HandController` and `HandModel` using Fusion RPCs for server-authoritative card plays.
- **API Robustness**: Resolved `HttpService` validation errors (422) by replacing anonymous payloads with concrete `LoginRequest` and `RegisterRequest` models for `JsonUtility` compatibility.
- **Compilation Repair**: Fixed severe namespace and interface discrepancies in `BattleSetup` and `Account` features caused by previous domain reloads.
### [2026-05-07] - Batch 7: Gameplay Subsystems Refactor & Synchronization
- **Compilation Repair**: Resolved severe `CS0101` and `CS0111` duplicate definition errors across `GameState` and `Board` subsystems.
- **Interface Consolidation**: Cleaned up `IGameStateSubsystem.cs` and `IBoardSubsystem.cs`, moving core interfaces to dedicated files and standardizing inheritance from `IModel` and `IController`.
- **GameState Networking**: Implemented full `IGameStateModel` with networked synchronization for `CurrentTurn`, `CurrentPhase`, and `MatchTimer` using Fusion `ChangeDetector`.
- **Board Networking**: Implemented `BoardModel` with a networked occupancy grid (`NetworkDictionary<int, NetworkString>`) and an RPC bridge for authoritative unit placement.
- **Gameplay Parity**: Fully implemented Models and Controllers for `FusePhase`, `Combat`, `DrawPhase`, and `MatchResult` with networked state synchronization and Zenject lifecycle support.
- **UI Warnings**: Resolved `UIPanel` field duplication warnings (`_closeButton`) in `ShopItemContextPopup` and `DeckItemContextPopup` by leveraging base class functionality.
