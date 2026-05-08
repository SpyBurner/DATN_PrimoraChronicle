using Zenject;

public class FusePhaseController : IFusePhaseController
{
    [Inject] private readonly IFusePhaseModel _model;
    [Inject] private readonly IDebugLogger _debugLogger;

    public void Initialize() { }
    public void Dispose() { }

    public void SetUnits(string primaryId, string secondaryId)
    {
        _debugLogger.Log($"FusePhaseController: SetUnits {primaryId}, {secondaryId}");
    }

    public void Fuse()
    {
        _debugLogger.Log("FusePhaseController: Fuse");
    }

    public void Cancel()
    {
        _debugLogger.Log("FusePhaseController: Cancel");
    }
}
