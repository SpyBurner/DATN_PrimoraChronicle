using System.Collections.Generic;
using Fusion;

public interface IPlayerCardZoneController : IController
{
    void RequestDraw(PlayerRef p, int count);
    void RequestKeepCards(PlayerRef p, IReadOnlyList<string> keep);
    void RequestPlayMainPhaseSpell(string cardId, HexCoord target);
    void RegisterBridge(IPlayerCardZoneNetworkBridge bridge);
    void OnAuthoritativeStateReceived(PlayerCardZonePrivateData data);
}
