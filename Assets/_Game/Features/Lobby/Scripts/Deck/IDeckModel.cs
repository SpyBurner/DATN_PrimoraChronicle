using System.Collections.Generic;
using UnityObservables;

public interface IDeckModel : IModel
{
    Observable<List<DeckSummaryData>> Decks { get; }
    Observable<int> DeckCount { get; }

    public void SetDecks(List<DeckSummaryData> decks);
}

