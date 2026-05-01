using System.Collections.Generic;
using Core;
using UnityObservables;

internal class CardLoadingManagerModel : ICardLoadingManagerModel
{
    private Observable<Dictionary<string, CardSO>> _cardsById = new(new());
    private Observable<Dictionary<string, ChampionCardSO>> _championCardsList = new(new());
    private Observable<Dictionary<string, SpellCardSO>> _spellCardList = new(new());
    private Observable<Dictionary<string, TroopCardSO>> _troopCardList = new(new());

    public Observable<Dictionary<string, CardSO>> CardsById => _cardsById;
    public Observable<Dictionary<string, ChampionCardSO>> ChampionCardsList => _championCardsList;
    public Observable<Dictionary<string, SpellCardSO>> SpellCardList => _spellCardList;
    public Observable<Dictionary<string, TroopCardSO>> TroopCardList => _troopCardList;

    public void Initialize()
    {
    }

    public void Dispose()
    {
        _cardsById?.Value?.Clear();
        _championCardsList?.Value?.Clear();
        _spellCardList?.Value?.Clear();
        _troopCardList?.Value?.Clear();

        _cardsById = null;
        _championCardsList = null;
        _spellCardList = null;
        _troopCardList = null;
    }
}