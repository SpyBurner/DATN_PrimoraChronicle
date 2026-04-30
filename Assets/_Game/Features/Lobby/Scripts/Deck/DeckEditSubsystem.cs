using System.Collections.Generic;
using System;
using Core;
using Zenject;

public class DeckEditSubsystem : IDeckEditSubsystem, IInitializable, IDisposable
{
    [Inject] private readonly IDeckEditController _controller;

    public void Initialize() { }
    public void Dispose() { }

    public DeckSO GetSelectedDeck() => _controller.GetSelectedDeck();

    public ChampionCardSO GetChampionCard() => _controller.GetChampionCard();

    public IReadOnlyList<CardSO> GetDeckCards() => _controller.GetDeckCards();

    public IReadOnlyList<CardSO> GetChampionCards() => _controller.GetChampionCards();

    public void StoreSelectedDeck(DeckSO deckSO) => _controller.StoreSelectedDeck(deckSO);
}