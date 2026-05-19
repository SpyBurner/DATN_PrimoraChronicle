using System.Threading.Tasks;

public interface IMatchResultController : IController
{
    Task ReturnToLobby();
    void RegisterBridge(IMatchResultNetworkBridge bridge);
    void OnAuthoritativeStateReceived(GameMatchResult data);
}
