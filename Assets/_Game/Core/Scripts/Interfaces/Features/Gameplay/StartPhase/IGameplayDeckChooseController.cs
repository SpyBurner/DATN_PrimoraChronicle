using System.Threading.Tasks;

public interface IGameplayDeckChooseController : IController
{
    void StageSelection(DeckSummaryData summary);
    Task ConfirmSelection();
    Task AutoConfirmLastDeck();

    void RegisterBridge(IGameplayDeckChooseNetworkBridge bridge);
    void OnAuthoritativeStateReceived(GameplayDeckChooseStateData data);
}
