using System.Collections.Generic;
using UnityObservables;

internal class DeckModel : IDeckModel
{
    private Observable<List<DeckSummaryData>> _decks = new(new List<DeckSummaryData>());
    private Observable<int> _deckCount = new(0);

    public Observable<List<DeckSummaryData>> Decks { get => _decks; }
    public Observable<int> DeckCount { get => _deckCount; }

    public void Initialize() { }

    public void Dispose()
    {
        _decks.Value.Clear();
        _deckCount.Value = 0;
    }

    public void SetDecks(List<DeckSummaryData> decks)
    {
        _decks.Value = new List<DeckSummaryData>(decks);
        _deckCount.Value = decks?.Count ?? 0;
    }
}
