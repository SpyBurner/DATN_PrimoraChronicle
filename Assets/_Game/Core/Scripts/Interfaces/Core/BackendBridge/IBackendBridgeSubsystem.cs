using System.Threading.Tasks;
using UnityEngine.Events;

public interface IBackendBridgeSubsystem : ISubsystem
{
    event UnityAction<StartSessionCommand> StartSessionReceived;
    event UnityAction ForceEndMatchReceived;

    Task ReportMatchResultAsync(MatchResultData result);
    Task ReportPlayerDisconnectedAsync(string userId);
    Task NotifyMatchCreatedAsync(string sessionName, string player1UserId, string player2UserId);
}
