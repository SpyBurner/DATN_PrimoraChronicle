using Fusion;
using UnityObservables;

public class GameStateModel : NetworkBehaviour, IGameStateModel
{
    private ChangeDetector _changeDetector;

    [Networked] public int NetworkedTurn { get; set; }
    [Networked] public string NetworkedPhase { get; set; }
    [Networked] public int NetworkedTimer { get; set; }

    private Observable<int> _currentTurn = new(0);
    private Observable<string> _currentPhase = new(string.Empty);
    private Observable<int> _matchTimer = new(0);

    public Observable<int> CurrentTurn { get => _currentTurn; }
    public Observable<string> CurrentPhase { get => _currentPhase; }
    public Observable<int> MatchTimer { get => _matchTimer; }

    public void Initialize() { }

    public void Dispose()
    {
        _currentTurn.Value = 0;
        _currentPhase.Value = string.Empty;
        _matchTimer.Value = 0;
    }

    public override void Spawned()
    {
        // Using Fusion's SimulationState change detector pattern
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        _currentTurn.Value = NetworkedTurn;
        _currentPhase.Value = NetworkedPhase;
        _matchTimer.Value = NetworkedTimer;
    }

    public override void Render()
    {
        if (_changeDetector == null) return;

        // Detect properties changed via network sync
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(NetworkedTurn):
                    _currentTurn.Value = NetworkedTurn;
                    break;
                case nameof(NetworkedPhase):
                    _currentPhase.Value = NetworkedPhase;
                    break;
                case nameof(NetworkedTimer):
                    _matchTimer.Value = NetworkedTimer;
                    break;
            }
        }
    }
}
