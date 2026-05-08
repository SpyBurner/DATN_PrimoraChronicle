using System.Threading.Tasks;
using Core;

public interface IDeckBuildController : IController
{
    Task LoadDeck(string deckId);
    Task CreateEmptyDeck();
    void AddCardToDeck(CardSO card);
    void RemoveCardFromDeck(CardSO card);
    Task SaveDeck();
}
