using UnityObservables;
using System.Collections.Generic;

public interface IDeckBuildModel : IModel
{
    Observable<List<string>> DeckCards { get; }
    Observable<int> DeckSize { get; }
    Observable<bool> IsValid { get; }

    void SetDeckCards(List<string> cards);
    void AddCard(string cardId);
    void RemoveCard(string cardId);
}
