using System.Collections.Generic;
using Core;

public interface IDeckSubsystem : ISubsystem
{
    void EditDeck(DeckSO deckSO);
    IReadOnlyList<DeckSO> LoadDecks();
}
