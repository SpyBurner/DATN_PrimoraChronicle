using System.Threading.Tasks;
using UnityEngine.Events;

public interface IMatchMakingSubsystem : ISubsystem
{
    event UnityAction<string> StatusChanged;
    event UnityAction<int> TimerChanged;

    Task StartMatchmaking();
    Task CancelMatchmaking();
    Task AcceptMatch();
    Task RejectMatch();
}
