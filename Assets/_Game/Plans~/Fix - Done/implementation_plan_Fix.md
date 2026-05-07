# Deck & DeckEdit Subsystem Overhaul (P1 Compliance)

This plan completely scraps the `DeckSO` anti-pattern introduced in commit `eb29021` and implements the **Deck Choosing** and **Deck Editing** features as two completely standalone subsystems, adhering to MVVM+Zenject rules defined in P1.

## User Review Required

> [!WARNING]
> **Data Loss for Local Tests**: This change will remove the local `.json` file saving functionality (`SaveToJsonFile`, `System.IO.Directory`) previously bundled in `DeckSO`. Decks will now be strictly treated as API data structures.
> **Asset Deletion**: `DeckSO.cs` will be completely removed. Existing `DeckSO` scriptable objects in the project will become invalid. 

## Open Questions

> [!NOTE]
> 1. Since the Deck Edit Subsystem needs a `deckId` to initialize and fetch data from the backend, how should this `deckId` be passed from the Lobby UI (when clicking a Deck in the Deck Panel) to the Deck Edit Panel? Currently, `DeckSubsystem` has a `SelectedDeck` concept. Should `DeckSubsystem` expose an event `event UnityAction<string> EditDeckRequested` that the `UIManager` listens to in order to open the `DeckEditPanel` and inject/pass the `deckId` to `DeckEditSubsystem`?
> 2. Since the backend APIs might not exist yet, I will simulate the HTTP responses in the Controllers for now so the UI can be tested. Is this acceptable?

## Proposed Architecture

---

### Core Data Structures

#### [NEW] `DeckSummaryData.cs` (Assets/_Game/Features/Lobby/Scripts/Deck/DeckSummaryData.cs)
For the Deck Choosing phase. Contains only the minimal data required.
- `public string Id;`
- `public string Name;`

#### [NEW] `DeckDetailData.cs` (Assets/_Game/Features/Lobby/Scripts/Deck/DeckDetailData.cs)
For the Deck Edit phase. The backend response format for a specific deck.
- `public string Id;`
- `public string Name;`
- `public List<string> CardIds;` (Backend only returns pure card IDs, no types)

#### [DELETE] `DeckSO.cs` (Assets/_Game/Core/Scripts/SOScript/DeckSO/DeckSO.cs)
Completely remove the ScriptableObject implementation and its file I/O operations.

---

### 1. Deck Choosing Subsystem (`DeckSubsystem`)

Responsible strictly for querying the list of decks and providing their summaries.

#### [MODIFY] `IDeckModel.cs` / `DeckModel.cs`
- Replace `Observable<List<DeckSO>> Decks` with `Observable<List<DeckSummaryData>> Decks`.

#### [MODIFY] `IDeckController.cs` / `DeckController.cs`
- `LoadDecks()`: Calls BE API to fetch all decks (returns only `id` and `name`).
- Populates `DeckModel.Decks`.

#### [MODIFY] `IDeckSubsystem.cs` / `DeckSubsystem.cs`
- Expose `event UnityAction<IReadOnlyList<DeckSummaryData>> DecksChanged`.

#### [MODIFY] `DeckPanel.cs` / `DeckButton.cs`
- Subscribes to `DecksChanged`.
- `DeckButton` is initialized with `DeckSummaryData` (shows the Name). When clicked, it should trigger the flow to open the Deck Edit panel for that `deckId`.

---

### 2. Deck Edit Subsystem (`DeckEditSubsystem`)

A standalone subsystem. Given a `deckId`, it queries the backend, resolves the local `CardSO`s, categorizes them, and manages the edit state.

#### [MODIFY] `IDeckEditModel.cs` / `DeckEditModel.cs`
- `Observable<string> CurrentDeckId`
- `Observable<string> CurrentDeckName`
- `Observable<List<CardSO>> DeckCards`
- `Observable<List<CardSO>> ChampionCards`
- `Observable<List<CardSO>> AvailableCards`

#### [MODIFY] `IDeckEditController.cs` / `DeckEditController.cs`
- `Task LoadDeck(string deckId)`: Queries BE API for all cards related to `deckId`. Receives a list of `cardId`s.
  - Queries `CardLoadingManagerSubsystem` with the `cardId`s to resolve `CardSO`s.
  - Maps and categorizes the resolved `CardSO`s (Troop, Spell, Champion) into the model.
- `void AddCardToDeck(CardSO card)`: Mutates model observables.
- `void RemoveCardFromDeck(CardSO card)`: Mutates model observables.
- `Task SaveDeck()`: Converts current model state back into a list of pure `cardId`s and sends to BE API.

#### [MODIFY] `IDeckEditSubsystem.cs` / `DeckEditSubsystem.cs`
- Strip all getter methods.
- Expose standard `event UnityAction` bindings for all observable model properties (e.g., `DeckCardsChanged`, `DeckNameChanged`).
- Expose `Task LoadDeck(string deckId)` so the UI can initialize it.

#### [MODIFY] `DeckEditPanel.cs`
- **Architecture Fix**: Remove synchronous `RenderCardDisplays()` calls after UI interactions. Bind card rendering strictly to subsystem events in `OnEnable()`.
- **UI Logic Fix**: Remove `DeckContainerChangeNotifier` and recursive child transforms iteration. Bind the empty deck placeholder state directly to the `DeckCardsChanged` event.

## Verification Plan

### Automated Tests
1. Verify codebase compiles without `DeckSO`.
2. Ensure Zenject installers are not broken.

### Manual Verification
1. Open Lobby scene.
2. Verify Decks can be loaded and displayed as summaries (Id/Name) in the Deck menu.
3. Open Deck Edit menu, verify the system correctly resolves pure Card IDs into actual `CardSO`s and displays them in their respective categories.
4. Verify the UI updates correctly via reactive event propagation when adding/removing cards.
5. Verify the empty placeholder appears safely when the deck is empty.
