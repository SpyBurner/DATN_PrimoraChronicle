using System.Threading.Tasks;

public interface IDeckBuildController : IController
{
    Task LoadDeck(string deckId);
    void AddCardToDeck(string cardId);
    void RemoveCardFromDeck(string cardId);
    Task SaveDeck(string deckName);
}
