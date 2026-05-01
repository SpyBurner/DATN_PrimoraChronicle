using System.Collections.Generic;
using System.Linq;
using Core;
using UnityEngine;
using Zenject;

internal class DeckEditController : IDeckEditController
{
    private const int MaxCopiesPerCard = 2;

    [Inject] private readonly IDeckEditModel _model;
    [Inject] private readonly ICardLoadingManagerSubsystem _cardLoadingManager;

    public void Initialize()
    {
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

    public IReadOnlyList<CardSO> GetAvailableCards()
    {
        return _model.AvailableCards;
    }

    public bool TryAddCardToSelectedDeck(CardSO card)
    {
        if (_model.SelectedDeck == null || card == null || string.IsNullOrWhiteSpace(card.ID))
        {
            return false;
        }

        List<string> targetCards = GetTargetCardIds(card);
        if (targetCards == null)
        {
            return false;
        }

        int existingCopies = targetCards.Count(cardId => string.Equals(cardId, card.ID));
        if (existingCopies >= MaxCopiesPerCard)
        {
            return false;
        }

        targetCards.Add(card.ID);
        RefreshRenderData();
        return true;
    }

    public bool TryRemoveCardFromSelectedDeck(CardSO card)
    {
        if (_model.SelectedDeck == null || card == null || string.IsNullOrWhiteSpace(card.ID))
        {
            return false;
        }

        List<string> targetCards = GetTargetCardIds(card);
        if (targetCards == null)
        {
            return false;
        }

        int existingIndex = targetCards.FindIndex(cardId => string.Equals(cardId, card.ID));
        if (existingIndex < 0)
        {
            return false;
        }

        targetCards.RemoveAt(existingIndex);
        RefreshRenderData();
        return true;
    }

    public bool SaveSelectedDeck()
    {
        if (_model.SelectedDeck == null)
        {
            return false;
        }

        try
        {
            _model.SelectedDeck.SaveToJsonFile();
            RefreshRenderData();
            return true;
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"Failed to save selected deck. {exception.Message}");
            return false;
        }
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
            _model.SetRenderData(null, null, GetAvailableSpellAndTroopCards());
            return;
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

        _model.SetRenderData(deckCards, championCards, GetAvailableSpellAndTroopCards());
    }

    private List<CardSO> GetAvailableSpellAndTroopCards()
    {
        List<CardSO> availableCards = new();
        availableCards.AddRange(_cardLoadingManager.GetTroopCardList().Values);
        availableCards.AddRange(_cardLoadingManager.GetSpellCardList().Values);
        return availableCards;
    }

    private List<string> GetTargetCardIds(CardSO card)
    {
        return card switch
        {
            TroopCardSO => _model.SelectedDeck.TroopCardsID,
            SpellCardSO => _model.SelectedDeck.SpellCardsID,
            _ => null,
        };
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
        return _cardLoadingManager.GetCard<TCard>(cardId);
    }
}