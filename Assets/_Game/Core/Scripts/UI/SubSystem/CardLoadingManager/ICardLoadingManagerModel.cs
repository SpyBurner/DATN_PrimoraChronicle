using System.Collections.Generic;
using Core;
using Core.GDS;
using UnityObservables;

public interface ICardLoadingManagerModel : IModel
{
    Observable<Dictionary<string, CardSO>> CardsById { get; }
    Observable<Dictionary<string, ChampionCardSO>> ChampionCardsList { get; }
    Observable<Dictionary<string, SpellCardSO>> SpellCardList { get; }
    Observable<Dictionary<string, TroopCardSO>> TroopCardList { get; }
    
    // GDS Data
    Observable<MasterGDSData> MasterData { get; }
}