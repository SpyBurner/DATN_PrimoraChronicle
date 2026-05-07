using System.Collections.Generic;
using Core;
using UnityObservables;

public interface IDeckModel : IModel
{
    Observable<List<DeckSO>> Decks { get; }
    Observable<int> DeckCount { get; }

    public void SetDecks(List<DeckSO> decks);
}

