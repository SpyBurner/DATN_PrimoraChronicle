using System;
using UnityEngine.Events;

public interface IMatchResultSubsystem : ISubsystem
{
    event UnityAction<bool> IsVictoryChanged;
    event UnityAction<int> GoldEarnedChanged;
    event UnityAction<int> RankProgressChanged;

    // Intent
    void ShowResult(bool victory, int gold, int rank);
    void BackToLobby();

    // Network registration
    void RegisterNetworkBridge(IMatchResultNetworkBridge bridge);

    // Authoritative sync
    void OnAuthoritativeStateReceived(MatchResultStateData data);
}
