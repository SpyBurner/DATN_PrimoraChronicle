using System.Collections.Generic;
using Fusion;
using UnityEngine.Events;

public interface IPlayerRosterSubsystem : ISubsystem
{
    event UnityAction<PlayerRef, int> HPChanged;
    event UnityAction<PlayerRef, string> NameChanged;
    event UnityAction<PlayerRef, string> UserIdChanged;

    IReadOnlyList<PlayerRef> AllPlayers { get; }
    int GetHP(PlayerRef p);
    string GetName(PlayerRef p);
    string GetUserId(PlayerRef p);

    void RegisterNetworkBridge(IPlayerRosterNetworkBridge bridge);
    void OnAuthoritativeStateReceived(PlayerRosterPublicData data);
}
