public struct FusePhaseStateData
{
    public bool IsActive;
    public string PrimaryUnitId;
    public string SecondaryUnitId;
}

public interface IFusePhaseNetworkBridge
{
    void SendSetUnitsRpc(string primaryId, string secondaryId);
    void SendFuseRpc();
    void SendCancelRpc();
}
