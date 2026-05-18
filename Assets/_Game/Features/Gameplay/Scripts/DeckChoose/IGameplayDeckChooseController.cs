using System.Threading.Tasks;

internal interface IGameplayDeckChooseController
{
    void Initialize();
    void Dispose();

    void StageSelection(DeckSummaryData summary);
    Task ConfirmSelection();
    Task AutoConfirmLastDeck();

    void RegisterBridge(IGameplayDeckChooseNetworkBridge bridge);
    void OnAuthoritativeStateReceived(GameplayDeckChooseStateData data);
}
