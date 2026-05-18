using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Events;

public interface IDeckSubsystem : ISubsystem
{
    event UnityAction<int> DeckCountChanged;
    event UnityAction<IReadOnlyList<DeckSummaryData>> DecksChanged;

    Task LoadDecks();
}
