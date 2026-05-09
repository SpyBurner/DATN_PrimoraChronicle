using System.Collections.Generic;
using System.Threading.Tasks;
using Core;
using Core.GDS;
using UnityEngine.Events;

public interface ICardLoadingManagerSubsystem : ISubsystem
{
    event UnityAction<IReadOnlyDictionary<string, CardSO>> CardsByIdChanged;
    Task LoadCardsAsync();
    IReadOnlyDictionary<string, CardSO> GetCardsById();
    IReadOnlyDictionary<string, ChampionCardSO> GetChampionCardsList();
    IReadOnlyDictionary<string, SpellCardSO> GetSpellCardList();
    IReadOnlyDictionary<string, TroopCardSO> GetTroopCardList();
    bool TryGetCard(string cardId, out CardSO card);
    T GetCard<T>(string cardId) where T : CardSO;
    MasterGDSData GetMasterGDSData();
    bool TryGetCardData(string stringId, out CardData data);
    bool TryGetSkillData(string stringId, out SkillData data);
    bool TryGetEffectData(string stringId, out StatusEffectData data);
}