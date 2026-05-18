using System.Threading.Tasks;
using UnityEngine.Events;

public interface IMatchMakingSubsystem : ISubsystem
{
    event UnityAction<string> StatusChanged;
    event UnityAction<int> TimerChanged;
    event UnityAction<MatchMakingPhase> PhaseChanged;

    MatchMakingPhase CurrentPhase { get; }

    Task StartAsHost();
    Task StartAsClient(string sessionName);
    Task CancelMatchmaking();
    Task AcceptMatch();
    Task RejectMatch();
}
