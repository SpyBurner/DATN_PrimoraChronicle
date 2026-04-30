using System.Collections.Generic;
using Core;
using UnityEngine;

public interface IDeckEditSubsystem : ISubsystem
{
    DeckSO GetSelectedDeck();
    ChampionCardSO GetChampionCard();
    IReadOnlyList<CardSO> GetDeckCards();
    IReadOnlyList<CardSO> GetChampionCards();

    void StoreSelectedDeck(DeckSO deckSO);
}