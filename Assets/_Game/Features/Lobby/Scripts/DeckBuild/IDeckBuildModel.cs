using UnityObservables;
using System.Collections.Generic;
using Core;

public interface IDeckBuildModel : IModel
{
    Observable<string> CurrentDeckId { get; }
    Observable<string> CurrentDeckName { get; }
    Observable<List<CardSO>> DeckCards { get; }
    Observable<List<CardSO>> ChampionCards { get; }
    Observable<List<CardSO>> ChampionGrantedCards { get; }
    Observable<List<CardSO>> AvailableCards { get; }
    Observable<int> DeckSize { get; }
    Observable<bool> IsValid { get; }
    Observable<string> ErrorMessage { get; }

    void SetCurrentDeck(string id, string name);
    void SetErrorMessage(string message);
    void SetAvailableCards(IEnumerable<CardSO> availableCards);
    void SetRenderData(IEnumerable<CardSO> deckCards, IEnumerable<CardSO> championCards, IEnumerable<CardSO> grantedCards, IEnumerable<CardSO> availableCards);
}
