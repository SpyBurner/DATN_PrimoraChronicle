using System.Threading.Tasks;
using UnityEngine.Events;

public interface IMatchMakingSubsystem : ISubsystem
{
    event UnityAction<bool> IsSearchingChanged;
    event UnityAction<string> StatusChanged;
    event UnityAction<int> QueuePositionChanged;

    Task StartMatchmaking();
    Task CancelMatchmaking();
}
