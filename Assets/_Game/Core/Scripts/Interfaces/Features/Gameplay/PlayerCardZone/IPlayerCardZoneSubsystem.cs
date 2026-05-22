using System.Collections.Generic;
using Fusion;
using UnityEngine.Events;

public interface IPlayerCardZoneSubsystem : ISubsystem
{
    // owner-only — fires only on the owning client
    event UnityAction<IReadOnlyList<string>> OwnHandChanged;

    IReadOnlyList<string> GetOwnHand();

    void RequestDraw(PlayerRef p, int count);
    void RequestKeepCards(PlayerRef p, IReadOnlyList<string> keep);
    void RequestPlayMainPhaseSpell(string cardId, HexCoord target);

    void RegisterNetworkBridge(IPlayerCardZoneNetworkBridge bridge);
    void OnAuthoritativeStateReceived(PlayerCardZonePrivateData data);
}
