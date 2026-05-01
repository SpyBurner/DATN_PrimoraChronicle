using System.Collections.Generic;
using Core;
using UnityEngine;
using Zenject;

internal class CardLoadingManagerController : ICardLoadingManagerController
{
    private const string CardResourcesPath = "CardSO";

    [Inject] private readonly ICardLoadingManagerModel _model;

    public void LoadCards()
    {
        Dictionary<string, CardSO> cardsById = new();
        Dictionary<string, ChampionCardSO> championCardsList = new();
        Dictionary<string, SpellCardSO> spellCardList = new();
        Dictionary<string, TroopCardSO> troopCardList = new();

        foreach (CardSO card in Resources.LoadAll<CardSO>(CardResourcesPath))
        {
            if (card == null || string.IsNullOrWhiteSpace(card.ID) || cardsById.ContainsKey(card.ID))
            {
                continue;
            }

            cardsById.Add(card.ID, card);

            switch (card)
            {
                case ChampionCardSO championCard:
                    championCardsList.Add(championCard.ID, championCard);
                    break;
                case SpellCardSO spellCard:
                    spellCardList.Add(spellCard.ID, spellCard);
                    break;
                case TroopCardSO troopCard:
                    troopCardList.Add(troopCard.ID, troopCard);
                    break;
            }
        }

        _model.CardsById.Value = cardsById;
        _model.ChampionCardsList.Value = championCardsList;
        _model.SpellCardList.Value = spellCardList;
        _model.TroopCardList.Value = troopCardList;
    }

    public IReadOnlyDictionary<string, CardSO> GetCardsById()
    {
        return _model.CardsById.Value;
    }

    public IReadOnlyDictionary<string, ChampionCardSO> GetChampionCardsList()
    {
        return _model.ChampionCardsList.Value;
    }

    public IReadOnlyDictionary<string, SpellCardSO> GetSpellCardList()
    {
        return _model.SpellCardList.Value;
    }

    public IReadOnlyDictionary<string, TroopCardSO> GetTroopCardList()
    {
        return _model.TroopCardList.Value;
    }

    public bool TryGetCard(string cardId, out CardSO card)
    {
        card = null;

        if (string.IsNullOrWhiteSpace(cardId))
        {
            return false;
        }

        return _model.CardsById.Value.TryGetValue(cardId, out card);
    }

    public T GetCard<T>(string cardId) where T : CardSO
    {
        return TryGetCard(cardId, out CardSO card) ? card as T : null;
    }
}