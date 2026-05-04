using System.Collections.Generic;
using System.Threading.Tasks;
using Core;

public interface IDeckController : IController
{
    Task LoadDecks();
    void SelectDeck(DeckSO deckSO);
}
