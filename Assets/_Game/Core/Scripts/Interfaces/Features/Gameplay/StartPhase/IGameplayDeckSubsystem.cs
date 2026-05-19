using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Events;

public interface IGameplayDeckSubsystem : ISubsystem
{
    event UnityAction<IReadOnlyList<DeckSummaryData>> DecksChanged;
    Task LoadDecks();
}
