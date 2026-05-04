using System.Threading.Tasks;
using UnityEngine.Events;

public interface IDeckBuildSubsystem : ISubsystem
{
    event UnityAction<int> DeckSizeChanged;
    event UnityAction<bool> IsValidChanged;

    Task LoadDeck(string deckId);
    void AddCardToDeck(string cardId);
    void RemoveCardFromDeck(string cardId);
    Task SaveDeck(string deckName);
}
