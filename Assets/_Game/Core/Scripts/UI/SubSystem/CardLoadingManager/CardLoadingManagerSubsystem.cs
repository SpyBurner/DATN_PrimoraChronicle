using System;
using System.Collections.Generic;
using Core;
using UnityEngine.Events;
using Zenject;

public class CardLoadingManagerSubsystem : ICardLoadingManagerSubsystem
{
    [Inject] private readonly ICardLoadingManagerController _controller;
    [Inject] private readonly ICardLoadingManagerModel _model;

    public event UnityAction<IReadOnlyDictionary<string, CardSO>> CardsByIdChanged;

    public void Initialize()
    {
        if (_model?.CardsById != null)
        {
            _model.CardsById.OnChanged += HandleCardsByIdChanged;
        }

        _controller.LoadCards();
    }

    public void Dispose()
    {
        if (_model?.CardsById != null)
        {
            _model.CardsById.OnChanged -= HandleCardsByIdChanged;
        }
    }

    public IReadOnlyDictionary<string, CardSO> GetCardsById()
    {
        return _controller.GetCardsById();
    }

    public IReadOnlyDictionary<string, ChampionCardSO> GetChampionCardsList()
    {
        return _controller.GetChampionCardsList();
    }

    public IReadOnlyDictionary<string, SpellCardSO> GetSpellCardList()
    {
        return _controller.GetSpellCardList();
    }

    public IReadOnlyDictionary<string, TroopCardSO> GetTroopCardList()
    {
        return _controller.GetTroopCardList();
    }

    public bool TryGetCard(string cardId, out CardSO card)
    {
        return _controller.TryGetCard(cardId, out card);
    }

    public T GetCard<T>(string cardId) where T : CardSO
    {
        return _controller.GetCard<T>(cardId);
    }

    private void HandleCardsByIdChanged()
    {
        try
        {
            CardsByIdChanged?.Invoke(_model.CardsById.Value);
        }
        catch (Exception)
        {
        }
    }
}