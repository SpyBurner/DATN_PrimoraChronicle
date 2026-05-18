using System.Threading.Tasks;
using UnityEngine.Events;

public interface IGameplayDeckChooseSubsystem
{
    event UnityAction<bool> IsReadyChanged;
    event UnityAction<string> SelectedDeckIdChanged;

    void StageSelection(DeckSummaryData summary);
    Task ConfirmSelection();
    Task AutoConfirmLastDeck();

    // Called by GameplayDeckChooseNetworkView after Spawned()
    void RegisterNetworkBridge(IGameplayDeckChooseNetworkBridge bridge);

    // Called by GameplayDeckChooseNetworkView from Render()
    void OnAuthoritativeStateReceived(GameplayDeckChooseStateData data);

    void Initialize();
    void Dispose();
}
