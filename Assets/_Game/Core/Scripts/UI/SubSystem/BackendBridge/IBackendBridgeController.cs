using System.Threading.Tasks;

internal interface IBackendBridgeController : IController
{
    void ClearPendingStartSession();
    Task ReportMatchResultAsync(MatchResultData result);
    Task ReportPlayerDisconnectedAsync(string userId);
}
