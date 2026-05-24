using System.Collections.Generic;
using Fusion;
using UnityEngine.Events;

public interface IPlayerCardZoneSubsystem : ISubsystem
{
    event UnityAction<PlayerRef, IReadOnlyList<string>> HandChanged;
    event UnityAction<PlayerRef, int> DeckCountChanged;
    event UnityAction<PlayerRef, int> DiscardCountChanged;
    event UnityAction<PlayerRef, int> HPChanged;
    event UnityAction<PlayerRef, int> DrawPhaseNewCardsChanged;
    event UnityAction<PlayerRef, bool> DrawPhaseConfirmedChanged;

    IReadOnlyList<string> GetHand(PlayerRef player);
    int GetDeckCount(PlayerRef player);
    int GetDiscardCount(PlayerRef player);
    int GetHP(PlayerRef player);
    int GetDrawPhaseNewCards(PlayerRef player);
    bool GetDrawPhaseConfirmed(PlayerRef player);

    void RequestDraw(PlayerRef p, int count);
    void RequestKeepCards(PlayerRef p, IReadOnlyList<string> keep);
    void RequestPlayMainPhaseSpell(string cardId, HexCoord target);

    void RegisterNetworkBridge(IPlayerCardZoneNetworkBridge bridge);
    void OnAuthoritativeStateReceived(PlayerCardZonePrivateData data);
}
