using System.Collections.Generic;
using Core;
using UnityEngine.Events;

public interface ICardLoadingManagerSubsystem : ISubsystem
{
    event UnityAction<IReadOnlyDictionary<string, CardSO>> CardsByIdChanged;

    IReadOnlyDictionary<string, CardSO> GetCardsById();
    IReadOnlyDictionary<string, ChampionCardSO> GetChampionCardsList();
    IReadOnlyDictionary<string, SpellCardSO> GetSpellCardList();
    IReadOnlyDictionary<string, TroopCardSO> GetTroopCardList();
    bool TryGetCard(string cardId, out CardSO card);
    T GetCard<T>(string cardId) where T : CardSO;
}