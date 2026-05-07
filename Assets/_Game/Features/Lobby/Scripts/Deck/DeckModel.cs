using System.Collections.Generic;
using Core;
using UnityObservables;

internal class DeckModel : IDeckModel
{
    private Observable<List<DeckSO>> _decks = new(new List<DeckSO>());
    private Observable<int> _deckCount = new(0);

    public Observable<List<DeckSO>> Decks { get => _decks; }
    public Observable<int> DeckCount { get => _deckCount; }

    public void Initialize() { }

    public void Dispose()
    {
        _decks.Value.Clear();
        _deckCount.Value = 0;
    }

    public void SetDecks(List<DeckSO> decks)
    {
        _decks.Value = new List<DeckSO>(decks);
        _deckCount.Value = decks?.Count ?? 0;
    }
}
