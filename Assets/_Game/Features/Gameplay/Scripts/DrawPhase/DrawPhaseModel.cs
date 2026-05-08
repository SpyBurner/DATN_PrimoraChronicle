using Fusion;
using UnityObservables;

public class DrawPhaseModel : NetworkBehaviour, IDrawPhaseModel
{
    private ChangeDetector _changeDetector;

    [Networked] public int NetworkedCount { get; set; }
    [Networked] public NetworkBool NetworkedIsDrawing { get; set; }

    private Observable<int> _cardsToDraw = new(0);
    public Observable<int> CardsToDraw => _cardsToDraw;

    private Observable<bool> _isDrawing = new(false);
    public Observable<bool> IsDrawing => _isDrawing;

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
        _cardsToDraw.Value = NetworkedCount;
        _isDrawing.Value = NetworkedIsDrawing;
    }
}
