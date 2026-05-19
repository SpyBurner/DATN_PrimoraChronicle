using System;
using System.Collections.Generic;
using Fusion;

public interface IPlayerCardZoneModel : IModel
{
    event Action<PlayerRef, int> HPChanged;
    event Action<PlayerRef, IReadOnlyList<string>> HandChanged;
    event Action<PlayerRef, int> DeckCountChanged;
    event Action<PlayerRef, int> DiscardCountChanged;
    event Action<PlayerRef, string> PlayerNameChanged;

    IReadOnlyList<string> GetHand(PlayerRef player);
    int GetDeckCount(PlayerRef player);
    int GetDiscardCount(PlayerRef player);
    int GetHP(PlayerRef player);
    string GetPlayerName(PlayerRef player);

    void ApplyState(PlayerCardZoneData data);
}
