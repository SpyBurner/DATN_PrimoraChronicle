using System.Collections.Generic;
using Core;
using UnityEngine;

public interface IDeckEditSubsystem : ISubsystem
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