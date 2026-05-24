using Zenject;

internal class TileEffectController : ITileEffectController
{
    [Inject] private readonly ITileEffectModel _model;
    [Inject] private readonly IDebugLogger _logger;

    private ITileEffectNetworkBridge _bridge;

    public void Initialize() { }

    public void Dispose() => _bridge = null;

    public void RegisterBridge(ITileEffectNetworkBridge bridge)
    {
        _bridge = bridge;
        _logger.Log("LOG_TILEEFFECT", nameof(TileEffectController), $"Bridge {(bridge == null ? "unregistered" : "registered")}.");
    }

    public void OnEffectReceived(TileEffectInstance instance) => _model.ApplyEffect(instance);

    public void OnEffectRemovedAt(HexCoord coord) => _model.RemoveEffect(coord);
}
