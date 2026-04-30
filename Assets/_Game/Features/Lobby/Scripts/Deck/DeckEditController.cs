using System.Collections.Generic;
using Core;
using Zenject;
using UnityEngine;

internal class DeckEditController : IDeckEditController
{
    private const string CardResourcesPath = "CardSO";

    [Inject] private readonly IDeckEditModel _model;

    private readonly Dictionary<string, CardSO> _cardsById = new();

    public void Initialize()
    {
        RebuildCardLookup();
        RefreshRenderData();
    }

    public DeckSO GetSelectedDeck()
    {
        return _model.SelectedDeck;
    }

    public ChampionCardSO GetChampionCard()
    {
        if (_model.SelectedDeck == null)
        {
            return null;
        }

        if (_cardsById.Count == 0)
        {
            RebuildCardLookup();
        }

        return ResolveCard<ChampionCardSO>(_model.SelectedDeck.ChampionCardID);
    }

    public IReadOnlyList<CardSO> GetDeckCards()
    {
        return _model.DeckCards;
    }

    public IReadOnlyList<CardSO> GetChampionCards()
    {
        return _model.ChampionCards;
    }

    public void StoreSelectedDeck(DeckSO deckSO)
    {
        _model.SetSelectedDeck(deckSO);
        RefreshRenderData();
    }

    private void RefreshRenderData()
    {
        if (_model.SelectedDeck == null)
        {
            _model.SetRenderData(null, null);
            return;
        }

        if (_cardsById.Count == 0)
        {
            RebuildCardLookup();
        }

        List<CardSO> deckCards = ResolveCards(_model.SelectedDeck.TroopCardsID);
        deckCards.AddRange(ResolveCards(_model.SelectedDeck.SpellCardsID));

        List<CardSO> championCards = new();
        ChampionCardSO championCard = ResolveCard<ChampionCardSO>(_model.SelectedDeck.ChampionCardID);
        if (championCard != null)
        {
            AddChampionCards(championCards, championCard.ChampionTroopCards);
            AddChampionCards(championCards, championCard.ChampionSpellCards);
        }

        _model.SetRenderData(deckCards, championCards);
    }

    private List<CardSO> ResolveCards(IEnumerable<string> cardIds)
    {
        List<CardSO> cards = new();
        if (cardIds == null)
        {
            return cards;
        }

        foreach (string cardId in cardIds)
        {
            CardSO card = ResolveCard<CardSO>(cardId);
            if (card != null)
            {
                cards.Add(card);
            }
        }

        return cards;
    }

    private static void AddChampionCards<TCard>(List<CardSO> destination, IEnumerable<TCard> cards) where TCard : CardSO
    {
        if (destination == null || cards == null)
        {
            return;
        }

        foreach (TCard card in cards)
        {
            if (card != null)
            {
                destination.Add(card);
            }
        }
    }

    private TCard ResolveCard<TCard>(string cardId) where TCard : CardSO
    {
        if (string.IsNullOrWhiteSpace(cardId) || !_cardsById.TryGetValue(cardId, out CardSO card))
        {
            return null;
        }

        return card as TCard;
    }

    private void RebuildCardLookup()
    {
        _cardsById.Clear();

        foreach (CardSO card in Resources.LoadAll<CardSO>(CardResourcesPath))
        {
            if (card == null || string.IsNullOrWhiteSpace(card.ID) || _cardsById.ContainsKey(card.ID))
            {
                continue;
            }

            _cardsById.Add(card.ID, card);
        }
    }
}