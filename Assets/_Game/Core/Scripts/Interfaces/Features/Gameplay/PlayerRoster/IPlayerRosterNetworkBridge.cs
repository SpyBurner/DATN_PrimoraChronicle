using Fusion;

public interface IPlayerRosterNetworkBridge
{
    void SendHPChangedRpc(PlayerRef owner, int newHP);
    void SendNameChangedRpc(PlayerRef owner, string name);
    void SendUserIdChangedRpc(PlayerRef owner, string userId);
}
