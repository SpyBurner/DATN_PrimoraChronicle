# Adaptation Plan — Rebase Assessment

## 1. ASSESSMENT SUMMARY
New commits introduce `CardLoadingManager`, `Deck`, and `DeckEdit` subsystems. Dependency Injection wiring established. 

**Status:** INCOMPLETE MVVM COMPLIANCE. Core architecture rules violated. Testing bypasses identified.

## 2. PROBLEM IDENTIFICATION

### 2.1 DECK CONTROLLER (JSON READING PROBLEM)
**[problem]** `DeckController.LoadDecks()` reads local `.json` files using `System.IO.Directory` and `File.ReadAllText`.
**[cause]** Temporary testing bypass implemented to simulate database/server response.
**[fix]** Replace local JSON I/O with API calls via the upcoming `HttpService` subsystem.

### 2.2 DECK & DECK EDIT MODELS (MVVM VIOLATION)
**[problem]** `DeckModel` and `DeckEditModel` use standard C# properties (e.g., `private List<DeckSO> _decks;`, `public IReadOnlyList<CardSO> DeckCards`) instead of `Observable<T>`.
**[cause]** Standard POCO implementation instead of reactive state containers.
**[fix]** Convert all data properties to `Observable<T>` (e.g., `Observable<List<CardSO>>`).

### 2.3 DECK EDIT PANEL (EVENT LOOP BYPASS)
**[problem]** `DeckEditPanel` manually calls `RenderCardDisplays()` immediately after executing a controller action (`TryAddCardToSelectedDeck`).
**[cause]** View orchestrates UI updates synchronously rather than reacting to model state changes.
**[fix]** View must subscribe to model `OnChanged` events via the subsystem interface. Controller modifies model → Model fires event → View reacts. Controller methods must return `void` or `Task`, not success booleans for the View to parse.

### 2.4 DECK EDIT PANEL (PLACEHOLDER LOGIC)
**[problem]** `DeckEditPanel` uses a custom `DeckContainerChangeNotifier` and deeply iterates Transform children to manage an "EmptyCardDisplay" placeholder.
**[cause]** View is managing complex state logic via the GameObject hierarchy.
**[fix]** Bind placeholder visibility strictly to model state. Check `DeckCards.Value.Count == 0` during the `OnChanged` event and simply toggle a dedicated placeholder GameObject's active state.

### 2.5 CARD LOADING MANAGER MODEL (OBSERVABLE MUTATION)
**[problem]** `CardLoadingManagerModel` uses `Observable<Dictionary<...>>`.
**[cause]** `Observable<T>` triggers change events only when `.Value` is reassigned. Modifying the dictionary directly (`.Value.Add()`) will not notify Views.
**[fix]** Controller correctly reassigns `.Value = newDictionary` during `LoadCards()`. This is acceptable for initialization, but any future dynamic additions must assign a new collection or use an `ObservableDictionary`. No immediate fix required for static initialization.

## 3. POSITIVE IMPLEMENTATIONS (KEEP)
- Subsystem separation (Deck vs DeckEdit vs CardLoading) correctly follows global/scoped architecture.
- `[Inject]` properly utilizes interfaces rather than concrete classes.
- UI component encapsulation (`CardDisplay` prefab usage) is correct.
