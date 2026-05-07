using System.Collections.Generic;
using System.Threading.Tasks;
using Core;
using UnityEngine.Events;

public interface IDeckBuildSubsystem : ISubsystem
{
    event UnityAction<string> CurrentDeckIdChanged;
    event UnityAction<string> CurrentDeckNameChanged;
    event UnityAction<IReadOnlyList<CardSO>> DeckCardsChanged;
    event UnityAction<IReadOnlyList<CardSO>> ChampionCardsChanged;
    event UnityAction<IReadOnlyList<CardSO>> AvailableCardsChanged;
    event UnityAction<int> DeckSizeChanged;
    event UnityAction<bool> IsValidChanged;

    Task LoadDeck(string deckId);
    void AddCardToDeck(CardSO card);
    void RemoveCardFromDeck(CardSO card);
    Task SaveDeck();
}
