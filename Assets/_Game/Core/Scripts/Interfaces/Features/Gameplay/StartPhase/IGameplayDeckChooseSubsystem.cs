using System.Threading.Tasks;
using UnityEngine.Events;

public interface IGameplayDeckChooseSubsystem : ISubsystem
{
    event UnityAction<bool> IsReadyChanged;
    event UnityAction<string> SelectedDeckIdChanged;

    void StageSelection(DeckSummaryData summary);
    Task ConfirmSelection();
    Task AutoConfirmLastDeck();

    void RegisterNetworkBridge(IGameplayDeckChooseNetworkBridge bridge);
    void OnAuthoritativeStateReceived(GameplayDeckChooseStateData data);
}
