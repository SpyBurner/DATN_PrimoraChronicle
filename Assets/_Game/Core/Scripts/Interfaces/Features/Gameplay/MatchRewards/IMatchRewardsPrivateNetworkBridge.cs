using Fusion;

public interface IMatchRewardsPrivateNetworkBridge
{
    void SendRewardsRpc(PlayerRef owner, int gold, int xp);
}
