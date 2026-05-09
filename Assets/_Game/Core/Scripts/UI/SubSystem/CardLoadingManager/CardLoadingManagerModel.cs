using System.Collections.Generic;
using Core;
using Core.GDS;
using UnityObservables;

internal class CardLoadingManagerModel : ICardLoadingManagerModel
{
    public Observable<Dictionary<string, CardSO>> CardsById { get; } = new(new Dictionary<string, CardSO>());
    public Observable<Dictionary<string, ChampionCardSO>> ChampionCardsList { get; } = new(new Dictionary<string, ChampionCardSO>());
    public Observable<Dictionary<string, SpellCardSO>> SpellCardList { get; } = new(new Dictionary<string, SpellCardSO>());
    public Observable<Dictionary<string, TroopCardSO>> TroopCardList { get; } = new(new Dictionary<string, TroopCardSO>());
    public Observable<MasterGDSData> MasterData { get; } = new(null);

    public void Initialize()
    {
    }

    public void Dispose()
    {
    }
}