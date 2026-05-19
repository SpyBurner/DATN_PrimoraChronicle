using System.Collections.Generic;
using Fusion;

public interface IPlayerCardZoneController : IController
{
    void RequestDraw(PlayerRef player, int count);
    void RequestKeepCards(PlayerRef player, IReadOnlyList<string> keep);
    void RequestPlayMainPhaseSpell(string cardId, HexCoord target);
    void RegisterBridge(IPlayerCardZoneNetworkBridge bridge);
    void OnAuthoritativeStateReceived(PlayerCardZoneData data);
}
