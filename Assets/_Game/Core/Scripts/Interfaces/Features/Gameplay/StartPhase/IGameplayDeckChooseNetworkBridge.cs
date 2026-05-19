public interface IGameplayDeckChooseNetworkBridge
{
    void SendConfirmRpc(string championId, string cardIdsJoined, int playerIndex, string playerName);
    void SendAutoConfirmRpc(int playerIndex);
}
