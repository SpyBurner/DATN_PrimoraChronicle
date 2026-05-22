using UnityEngine.Events;

public interface IMatchRewardsSubsystem : ISubsystem
{
    // owner-only — fires only on the owning client after AoI replication
    event UnityAction<int, int> OwnRewardsReceived; // (gold, xp)

    int OwnGold { get; }
    int OwnXP { get; }

    void RegisterNetworkBridge(IMatchRewardsPrivateNetworkBridge bridge);
    void OnAuthoritativeStateReceived(MatchRewardsPrivateData data);
}
