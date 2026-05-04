using System.Collections.Generic;
using Core;

public interface IDeckController : IController
{
    void EditDeck(DeckSO deckSO);
    IReadOnlyList<DeckSO> LoadDecks();
}
