using System.Collections.Generic;

public struct HandStateData
{
    public List<string> Cards;
}

public interface IHandNetworkBridge
{
    void SendPlayCardRpc(string cardId);
}
