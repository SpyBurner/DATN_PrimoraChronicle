using UnityObservables;

public class FusePhaseModel : IFusePhaseModel
{
    private Observable<bool> _isActive = new(false);
    public Observable<bool> IsActive => _isActive;

    private Observable<string> _primaryUnitId = new("");
    public Observable<string> PrimaryUnitId => _primaryUnitId;

    private Observable<string> _secondaryUnitId = new("");
    public Observable<string> SecondaryUnitId => _secondaryUnitId;

    public void Initialize() { }

    public void Dispose()
    {
        _isActive.Value = false;
        _primaryUnitId.Value = "";
        _secondaryUnitId.Value = "";
    }

    public void ApplyState(FusePhaseStateData data)
    {
        _isActive.Value = data.IsActive;
        _primaryUnitId.Value = data.PrimaryUnitId;
        _secondaryUnitId.Value = data.SecondaryUnitId;
    }
}
