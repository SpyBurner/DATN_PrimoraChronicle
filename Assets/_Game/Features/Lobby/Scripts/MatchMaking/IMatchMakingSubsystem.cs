using System.Threading.Tasks;
using UnityEngine.Events;

public interface IMatchMakingSubsystem : ISubsystem
{
    event UnityAction<bool> IsSearchingChanged;
    event UnityAction<string> StatusChanged;
    event UnityAction<int> QueuePositionChanged;
    event UnityAction<bool> IsMatchFoundChanged;
    event UnityAction<int> ConfirmationTimerChanged;

    bool IsSearching { get; }
    string Status { get; }
    int QueuePosition { get; }
    bool IsMatchFound { get; }
    int ConfirmationTimer { get; }

    Task StartMatchmaking();
    Task CancelMatchmaking();
    Task AcceptMatch();
    Task RejectMatch();
}
