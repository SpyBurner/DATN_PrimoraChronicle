public interface IStartPhaseController : IController
{
    void SetIsReady(bool ready);
    void AddChampion(int championId);
    void RemoveChampion(int championId);
    void RegisterBridge(IStartPhaseNetworkBridge bridge);
    void OnAuthoritativeStateReceived(StartPhaseStateData data);
}
