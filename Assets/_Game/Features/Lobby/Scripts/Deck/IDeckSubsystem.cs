using System.Collections.Generic;
using System.Threading.Tasks;
using Core;
using UnityEngine.Events;

public interface IDeckSubsystem : ISubsystem
{
    event UnityAction<int> DeckCountChanged;

    Task LoadDecks();
    void SelectDeck(DeckSO deckSO);
}
