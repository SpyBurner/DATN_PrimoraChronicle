using System;
using System.Collections.Generic;
using Fusion;

public interface IPlayerRosterModel : IModel
{
    event Action<PlayerRef, int> HPChanged;
    event Action<PlayerRef, string> NameChanged;
    event Action<PlayerRef, string> UserIdChanged;

    IReadOnlyList<PlayerRef> AllPlayers { get; }
    int GetHP(PlayerRef p);
    string GetName(PlayerRef p);
    string GetUserId(PlayerRef p);

    void ApplyState(PlayerRosterPublicData data);
}
