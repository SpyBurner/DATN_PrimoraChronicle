public interface IGameplayDeckChooseNetworkBridge
{
    void SendConfirmRpc(string championId, string cardIdsJoined, int playerIndex);
    void SendAutoConfirmRpc(int playerIndex);
}
