using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core;
using Core.GDS;
using UnityEngine.Events;
using Zenject;

public class CardLoadingManagerSubsystem : ICardLoadingManagerSubsystem
{
    [Inject] private readonly ICardLoadingManagerController _controller;
    [Inject] private readonly ICardLoadingManagerModel _model;

    public event UnityAction<IReadOnlyDictionary<string, CardSO>> CardsByIdChanged;

    public async void Initialize()
    {
        if (_model?.CardsById != null)
        {
            _model.CardsById.OnChanged += HandleCardsByIdChanged;
        }

        await LoadCardsAsync();
    }

    public void Dispose()
    {
        if (_model?.CardsById != null)
        {
            _model.CardsById.OnChanged -= HandleCardsByIdChanged;
        }
    }

    public async Task LoadCardsAsync()
    {
        await _controller.LoadCardsAsync();
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

    public MasterGDSData GetMasterGDSData()
    {
        return _controller.GetMasterGDSData();
    }

    public bool TryGetCardData(string stringId, out CardData data)
    {
        return _controller.TryGetCardData(stringId, out data);
    }

    public bool TryGetSkillData(string stringId, out SkillData data)
    {
        return _controller.TryGetSkillData(stringId, out data);
    }

    public bool TryGetEffectData(string stringId, out StatusEffectData data)
    {
        return _controller.TryGetEffectData(stringId, out data);
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