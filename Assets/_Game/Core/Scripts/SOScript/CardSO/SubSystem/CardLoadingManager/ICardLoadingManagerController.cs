using System.Collections.Generic;
using Core;

public interface ICardLoadingManagerController : IController
{
    void LoadCards();
    IReadOnlyDictionary<string, CardSO> GetCardsById();
    IReadOnlyDictionary<string, ChampionCardSO> GetChampionCardsList();
    IReadOnlyDictionary<string, SpellCardSO> GetSpellCardList();
    IReadOnlyDictionary<string, TroopCardSO> GetTroopCardList();
    bool TryGetCard(string cardId, out CardSO card);
    T GetCard<T>(string cardId) where T : CardSO;
}