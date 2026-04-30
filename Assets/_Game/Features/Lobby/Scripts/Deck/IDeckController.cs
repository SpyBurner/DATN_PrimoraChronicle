using System.Collections.Generic;
using Core;
using Zenject;

public interface IDeckController : IInitializable
{
    void EditDeck(DeckSO deckSO);
    IReadOnlyList<DeckSO> LoadDecks();
}
