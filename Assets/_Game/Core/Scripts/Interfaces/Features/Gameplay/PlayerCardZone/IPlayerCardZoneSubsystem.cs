using System.Collections.Generic;
using Fusion;
using UnityEngine.Events;

public interface IPlayerCardZoneSubsystem : ISubsystem
{
    event UnityAction<PlayerRef, IReadOnlyList<string>> HandChanged;
    event UnityAction<PlayerRef, int> DeckCountChanged;
    event UnityAction<PlayerRef, int> DiscardCountChanged;
    event UnityAction<PlayerRef, int> HPChanged;

    IReadOnlyList<string> GetHand(PlayerRef player);
    int GetDeckCount(PlayerRef player);
    int GetDiscardCount(PlayerRef player);
    int GetHP(PlayerRef player);

    void RequestDraw(PlayerRef player, int count);
    void RequestKeepCards(PlayerRef player, IReadOnlyList<string> keep);
    void RequestPlayMainPhaseSpell(string cardId, HexCoord target);

    void RegisterNetworkBridge(IPlayerCardZoneNetworkBridge bridge);
    void OnAuthoritativeStateReceived(PlayerCardZoneData data);
}
