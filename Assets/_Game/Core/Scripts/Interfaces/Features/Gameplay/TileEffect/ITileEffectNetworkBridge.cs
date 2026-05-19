public interface ITileEffectNetworkBridge
{
    void SendApplyEffectRpc(HexCoord coord, string effectId, int duration);
    void SendRemoveEffectRpc(HexCoord coord);
}
