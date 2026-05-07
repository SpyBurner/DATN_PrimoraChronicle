using Zenject;

public class BoardController : IBoardController
{
    [Inject] private readonly IBoardModel _model;
    [Inject] private readonly IDebugLogger _debugLogger;

    public void Initialize() { }
    public void Dispose() { }

    public void PlaceUnit(int cellIndex, string unitId)
    {
        _debugLogger.Log($"BoardController: PlaceUnit {unitId} at {cellIndex}");
        // Cast to concrete model to call RPC
        if (_model is BoardModel boardModel)
        {
            boardModel.RequestPlaceUnit(cellIndex, unitId);
        }
    }
}
