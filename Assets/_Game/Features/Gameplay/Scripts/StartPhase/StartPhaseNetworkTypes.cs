using System.Collections.Generic;

public struct StartPhaseStateData
{
    public List<int> SelectedChampions;
    public bool IsReady;
    public string Status;
}

public interface IStartPhaseNetworkBridge
{
    void SendSetIsReadyRpc(bool ready);
    void SendAddChampionRpc(int championId);
    void SendRemoveChampionRpc(int championId);
}
