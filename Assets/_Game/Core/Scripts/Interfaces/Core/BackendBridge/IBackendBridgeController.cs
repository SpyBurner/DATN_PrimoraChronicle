using System.Threading.Tasks;

public interface IBackendBridgeController : IController
{
    void ClearPendingStartSession();
    Task ReportMatchResultAsync(MatchResultData result);
    Task ReportPlayerDisconnectedAsync(string userId);
    Task NotifyMatchCreatedAsync(string sessionName, string player1UserId, string player2UserId);
}
