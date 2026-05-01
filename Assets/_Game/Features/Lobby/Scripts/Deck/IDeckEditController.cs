using System.Collections.Generic;
using Core;
using Zenject;

public interface IDeckEditController : IInitializable
{
    DeckSO GetSelectedDeck();
    ChampionCardSO GetChampionCard();
    IReadOnlyList<CardSO> GetDeckCards();
    IReadOnlyList<CardSO> GetChampionCards();
    IReadOnlyList<CardSO> GetAvailableCards();
    bool TryAddCardToSelectedDeck(CardSO card);
    bool TryRemoveCardFromSelectedDeck(CardSO card);
    bool SaveSelectedDeck();
    void StoreSelectedDeck(DeckSO deckSO);
}