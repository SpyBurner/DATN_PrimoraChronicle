using Zenject;

internal class BoardController : IBoardController
{
    private readonly IBoardModel _model;
    private readonly IDebugLogger _debugLogger;
    private IBoardNetworkBridge _bridge;

    public BoardController(IBoardModel model, IDebugLogger debugLogger)
    {
        _model = model;
        _debugLogger = debugLogger;
    }

    public void Initialize() { }
    public void Dispose() { }

    public void RegisterBridge(IBoardNetworkBridge bridge)
    {
        _bridge = bridge;
        _debugLogger.Log($"[BoardController] Bridge {(_bridge == null ? "unregistered" : "registered")}.");
    }

    public void PlaceUnit(int cellIndex, string unitId)
    {
        if (_bridge != null)
        {
            _bridge.SendPlaceUnitRpc(cellIndex, unitId);
        }
        else
        {
            _debugLogger.Log($"BoardController: PlaceUnit {unitId} at {cellIndex} (Local)");
            // Local path implementation
            // Since we don't have a direct setter in the model, we can simulate an authoritative update
            // However, the model only accepts BoardStateData.
        }
    }

    public void OnAuthoritativeStateReceived(BoardStateData data)
    {
        _model.ApplyState(data);
    }
}
