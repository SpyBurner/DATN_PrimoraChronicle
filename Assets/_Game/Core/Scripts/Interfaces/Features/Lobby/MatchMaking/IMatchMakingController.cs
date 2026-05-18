using System.Threading.Tasks;

public interface IMatchMakingController : IController
{
    Task JoinQueue();
    Task CancelMatchmaking();
    Task AcceptMatch();
    Task RejectMatch();
}
