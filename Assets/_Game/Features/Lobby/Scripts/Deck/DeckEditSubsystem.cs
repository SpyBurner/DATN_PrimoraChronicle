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

    public IReadOnlyList<CardSO> GetAvailableCards() => _controller.GetAvailableCards();

    public bool TryAddCardToSelectedDeck(CardSO card) => _controller.TryAddCardToSelectedDeck(card);

    public bool TryRemoveCardFromSelectedDeck(CardSO card) => _controller.TryRemoveCardFromSelectedDeck(card);

    public bool SaveSelectedDeck() => _controller.SaveSelectedDeck();

    public void StoreSelectedDeck(DeckSO deckSO) => _controller.StoreSelectedDeck(deckSO);
}