using Fusion;
using UnityObservables;
using UnityEngine;

public class GameStateModel : NetworkBehaviour, IGameStateModel
{
    private ChangeDetector _changeDetector;

    [Networked] public int NetworkedTurn { get; set; }
    [Networked] public NetworkString<_16> NetworkedPhase { get; set; }
    [Networked] public int NetworkedTimer { get; set; }

    private Observable<int> _currentTurn = new(0);
    public Observable<int> CurrentTurn => _currentTurn;

    private Observable<string> _currentPhase = new("None");
    public Observable<string> CurrentPhase => _currentPhase;

    private Observable<int> _matchTimer = new(0);
    public Observable<int> MatchTimer => _matchTimer;

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
            if (change == nameof(NetworkedTurn) || change == nameof(NetworkedPhase) || change == nameof(NetworkedTimer))
            {
                SyncState();
            }
        }
    }

    private void SyncState()
    {
        _currentTurn.Value = NetworkedTurn;
        _currentPhase.Value = NetworkedPhase.ToString();
        _matchTimer.Value = NetworkedTimer;
    }
}
