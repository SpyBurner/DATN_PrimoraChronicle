using System.Threading.Tasks;

internal interface IHandController : IController
{
    void PlayCard(string cardId);
    void RegisterBridge(IHandNetworkBridge bridge);
    void OnAuthoritativeStateReceived(HandStateData data);
}
