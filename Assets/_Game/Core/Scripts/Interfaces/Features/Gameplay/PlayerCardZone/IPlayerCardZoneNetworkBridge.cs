using System.Collections.Generic;
using Fusion;

public interface IPlayerCardZoneNetworkBridge
{
    void SendDrawRpc(PlayerRef p, int count);
    void SendKeepCardsRpc(PlayerRef p, string cardIdsJoined);
    void SendPlayMainPhaseSpellRpc(string cardId, HexCoord target);
}
