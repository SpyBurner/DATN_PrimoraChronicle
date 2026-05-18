public interface IMatchResultController : IController
{
    void ShowResult(bool victory, int gold, int rank);
    void BackToLobby();
    void RegisterBridge(IMatchResultNetworkBridge bridge);
    void OnAuthoritativeStateReceived(MatchResultStateData data);
}

