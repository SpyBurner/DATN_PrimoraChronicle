using System.Collections.Generic;
using Fusion;

public interface IPlayerCardZoneNetworkBridge
{
    void SendDrawRpc(PlayerRef player, int count);
    void SendKeepCardsRpc(PlayerRef player, string cardIdsJoined);
    void SendPlayMainPhaseSpellRpc(string cardId, HexCoord target);
}
