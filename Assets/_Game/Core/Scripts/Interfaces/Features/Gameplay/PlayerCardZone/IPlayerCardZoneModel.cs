using System;
using System.Collections.Generic;

public interface IPlayerCardZoneModel : IModel
{
    event Action<PlayerRef, int> HPChanged;
    event Action<PlayerRef, IReadOnlyList<string>> HandChanged;
    event Action<PlayerRef, int> DeckCountChanged;
    event Action<PlayerRef, int> DiscardCountChanged;
    event Action<PlayerRef, int> DrawPhaseNewCardsChanged;
    event Action<PlayerRef, bool> DrawPhaseConfirmedChanged;

    IReadOnlyList<string> GetHand(PlayerRef player);
    int GetDeckCount(PlayerRef player);
    int GetDiscardCount(PlayerRef player);
    int GetHP(PlayerRef player);
    int GetDrawPhaseNewCards(PlayerRef player);
    bool GetDrawPhaseConfirmed(PlayerRef player);

    void ApplyState(PlayerCardZonePrivateData data);
}
