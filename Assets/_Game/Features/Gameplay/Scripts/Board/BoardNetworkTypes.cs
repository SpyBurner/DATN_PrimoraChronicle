using System.Collections.Generic;

public struct BoardStateData
{
    public Dictionary<int, string> Grid;
}

public interface IBoardNetworkBridge
{
    void SendPlaceUnitRpc(int cellIndex, string unitId);
}
