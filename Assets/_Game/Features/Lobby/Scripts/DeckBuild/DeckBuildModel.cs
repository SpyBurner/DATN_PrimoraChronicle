using UnityObservables;
using System.Collections.Generic;

internal class DeckBuildModel : IDeckBuildModel
{
    private Observable<List<string>> _deckCards = new(new List<string>());
    private Observable<int> _deckSize = new(0);
    private Observable<bool> _isValid = new(false);

    public Observable<List<string>> DeckCards { get => _deckCards; }
    public Observable<int> DeckSize { get => _deckSize; }
    public Observable<bool> IsValid { get => _isValid; }

    public void Initialize() { }

    public void Dispose()
    {
        _deckCards.Value.Clear();
        _deckSize.Value = 0;
        _isValid.Value = false;
    }

    public void SetDeckCards(List<string> cards)
    {
        _deckCards.Value = new List<string>(cards);
        UpdateDeckSize();
        ValidateDeck();
    }

    public void AddCard(string cardId)
    {
        _deckCards.Value.Add(cardId);
        UpdateDeckSize();
        ValidateDeck();
    }

    public void RemoveCard(string cardId)
    {
        _deckCards.Value.Remove(cardId);
        UpdateDeckSize();
        ValidateDeck();
    }

    private void UpdateDeckSize()
    {
        _deckSize.Value = _deckCards.Value.Count;
    }

    private void ValidateDeck()
    {
        _isValid.Value = _deckSize.Value >= 20 && _deckSize.Value <= 30;
    }
}
