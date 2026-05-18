using System.Threading.Tasks;

public interface IMatchMakingController : IController
{
    Task JoinQueue();
    Task PollForMatch();
    Task ConnectToSession(string sessionName);
    Task CancelMatchmaking();
    Task AcceptMatch();
    Task RejectMatch();
}
