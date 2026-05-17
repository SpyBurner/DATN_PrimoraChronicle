using UnityObservables;
using System.Collections.Generic;
using Core;

internal class DeckBuildModel : IDeckBuildModel
{
    private Observable<string> _currentDeckId = new(string.Empty);
    private Observable<string> _currentDeckName = new(string.Empty);
    private Observable<List<CardSO>> _deckCards = new(new List<CardSO>());
    private Observable<List<CardSO>> _championCards = new(new List<CardSO>());
    private Observable<List<CardSO>> _championGrantedCards = new(new List<CardSO>());
    private Observable<List<CardSO>> _availableCards = new(new List<CardSO>());
    private Observable<int> _deckSize = new(0);
    private Observable<bool> _isValid = new(false);
    private Observable<string> _errorMessage = new(string.Empty);

    public Observable<string> CurrentDeckId { get => _currentDeckId; }
    public Observable<string> CurrentDeckName { get => _currentDeckName; }
    public Observable<List<CardSO>> DeckCards { get => _deckCards; }
    public Observable<List<CardSO>> ChampionCards { get => _championCards; }
    public Observable<List<CardSO>> ChampionGrantedCards { get => _championGrantedCards; }
    public Observable<List<CardSO>> AvailableCards { get => _availableCards; }
    public Observable<int> DeckSize { get => _deckSize; }
    public Observable<bool> IsValid { get => _isValid; }
    public Observable<string> ErrorMessage { get => _errorMessage; }

    public void Initialize() { }

    public void Dispose()
    {
        _currentDeckId.Value = string.Empty;
        _currentDeckName.Value = string.Empty;
        _deckCards.Value.Clear();
        _championCards.Value.Clear();
        _championGrantedCards.Value.Clear();
        _availableCards.Value.Clear();
        _deckSize.Value = 0;
        _isValid.Value = false;
        _errorMessage.Value = string.Empty;
    }

    public void SetErrorMessage(string message)
    {
        _errorMessage.Value = message ?? string.Empty;
    }

    public void SetCurrentDeck(string id, string name)
    {
        _currentDeckId.Value = id;
        _currentDeckName.Value = name;
    }

    public void SetRenderData(IEnumerable<CardSO> deckCards, IEnumerable<CardSO> championCards, IEnumerable<CardSO> grantedCards, IEnumerable<CardSO> availableCards)
    {
        _deckCards.Value = deckCards == null ? new List<CardSO>() : new List<CardSO>(deckCards);
        _championCards.Value = championCards == null ? new List<CardSO>() : new List<CardSO>(championCards);
        _championGrantedCards.Value = grantedCards == null ? new List<CardSO>() : new List<CardSO>(grantedCards);
        _availableCards.Value = availableCards == null ? new List<CardSO>() : new List<CardSO>(availableCards);
        UpdateDeckSize();
        ValidateDeck();
    }

    private void UpdateDeckSize()
    {
        _deckSize.Value = _deckCards.Value.Count;
    }

    private void ValidateDeck()
    {
        _isValid.Value = (_deckSize.Value == Constants.DECK_CARD_COUNT && _championCards.Value.Count == 1);
    }
}
