using Fusion;
using UnityObservables;

public class FusePhaseModel : NetworkBehaviour, IFusePhaseModel
{
    private ChangeDetector _changeDetector;

    [Networked] public NetworkBool NetworkedIsActive { get; set; }
    [Networked] public NetworkString<_16> NetworkedPrimaryId { get; set; }
    [Networked] public NetworkString<_16> NetworkedSecondaryId { get; set; }

    private Observable<bool> _isActive = new(false);
    public Observable<bool> IsActive => _isActive;

    private Observable<string> _primaryUnitId = new("");
    public Observable<string> PrimaryUnitId => _primaryUnitId;

    private Observable<string> _secondaryUnitId = new("");
    public Observable<string> SecondaryUnitId => _secondaryUnitId;

    public void Initialize() { }
    public void Dispose() { }

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        SyncState();
    }

    public override void Render()
    {
        if (_changeDetector == null) return;
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            SyncState();
            break;
        }
    }

    private void SyncState()
    {
        _isActive.Value = NetworkedIsActive;
        _primaryUnitId.Value = NetworkedPrimaryId.ToString();
        _secondaryUnitId.Value = NetworkedSecondaryId.ToString();
    }
}
