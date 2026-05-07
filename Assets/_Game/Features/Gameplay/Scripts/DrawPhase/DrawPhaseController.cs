using Zenject;

public class DrawPhaseController : IDrawPhaseController
{
    [Inject] private readonly IDrawPhaseModel _model;
    [Inject] private readonly IDebugLogger _debugLogger;

    public void Initialize() { }
    public void Dispose() { }

    public void StartDraw(int count)
    {
        _debugLogger.Log($"DrawPhaseController: StartDraw {count}");
    }

    public void CompleteDraw()
    {
        _debugLogger.Log("DrawPhaseController: CompleteDraw");
    }
}
