using System.Threading.Tasks;

public interface IHandController : IController
{
    void PlayCard(string cardId);
    void RegisterBridge(IHandNetworkBridge bridge);
    void OnAuthoritativeStateReceived(HandStateData data);
}
