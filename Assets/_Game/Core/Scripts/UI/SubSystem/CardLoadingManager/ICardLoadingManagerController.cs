using System.Collections.Generic;
using System.Threading.Tasks;
using Core;
using Core.GDS;

public interface ICardLoadingManagerController : IController
{
    void LoadCards();
    Task LoadCardsAsync();
    IReadOnlyDictionary<string, CardSO> GetCardsById();
    IReadOnlyDictionary<string, ChampionCardSO> GetChampionCardsList();
    IReadOnlyDictionary<string, SpellCardSO> GetSpellCardList();
    IReadOnlyDictionary<string, TroopCardSO> GetTroopCardList();
    bool TryGetCard(string cardId, out CardSO card);
    T GetCard<T>(string cardId) where T : CardSO;
    
    // GDS Data access
    MasterGDSData GetMasterGDSData();
    bool TryGetCardData(string stringId, out CardData data);
    bool TryGetSkillData(string stringId, out SkillData data);
    bool TryGetEffectData(string stringId, out StatusEffectData data);
}