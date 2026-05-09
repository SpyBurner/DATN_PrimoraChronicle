using UnityObservables;

public class GameStateModel : IGameStateModel
{
    private Observable<int> _currentTurn = new(0);
    public Observable<int> CurrentTurn => _currentTurn;

    private Observable<string> _currentPhase = new("");
    public Observable<string> CurrentPhase => _currentPhase;

    private Observable<int> _matchTimer = new(0);
    public Observable<int> MatchTimer => _matchTimer;

    public void Initialize() { }

    public void Dispose()
    {
        _currentTurn.Value = 0;
        _currentPhase.Value = "";
        _matchTimer.Value = 0;
    }

    public void ApplyState(GameStateStateData data)
    {
        _currentTurn.Value = data.CurrentTurn;
        _currentPhase.Value = data.CurrentPhase;
        _matchTimer.Value = data.MatchTimer;
    }
}
