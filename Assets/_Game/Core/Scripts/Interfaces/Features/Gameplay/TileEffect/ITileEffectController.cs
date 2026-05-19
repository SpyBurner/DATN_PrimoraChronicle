public interface ITileEffectController : IController
{
    void RegisterBridge(ITileEffectNetworkBridge bridge);
    void OnEffectReceived(TileEffectInstance instance);
    void OnEffectRemovedAt(HexCoord coord);
}
