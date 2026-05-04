using UnityEngine.Events;

public interface IGameStateSubsystem : ISubsystem
{
    event UnityAction<int> CurrentTurnChanged;
    event UnityAction<string> CurrentPhaseChanged;
    event UnityAction<int> MatchTimerChanged;

    void StartMatch();
    void EndTurn();
}
