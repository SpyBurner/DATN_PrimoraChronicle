using System.Threading.Tasks;
using UnityEngine.Events;

public interface IBackendBridgeSubsystem : ISubsystem
{
    event UnityAction<StartSessionCommand> StartSessionReceived;
    event UnityAction ForceEndMatchReceived;

    Task ReportMatchResultAsync(MatchResultData result);
    Task ReportPlayerDisconnectedAsync(string userId);
}
