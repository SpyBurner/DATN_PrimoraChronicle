using System.Collections.Generic;
using Core;

public interface IDeckModel : IModel
{
    IReadOnlyList<DeckSO> Decks { get; }
    void SetDecks(IReadOnlyList<DeckSO> decks);
}
