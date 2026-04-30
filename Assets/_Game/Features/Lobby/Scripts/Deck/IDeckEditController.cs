using System.Collections.Generic;
using Core;
using Zenject;

public interface IDeckEditController : IInitializable
{
    DeckSO GetSelectedDeck();
    ChampionCardSO GetChampionCard();
    IReadOnlyList<CardSO> GetDeckCards();
    IReadOnlyList<CardSO> GetChampionCards();
    void StoreSelectedDeck(DeckSO deckSO);
}