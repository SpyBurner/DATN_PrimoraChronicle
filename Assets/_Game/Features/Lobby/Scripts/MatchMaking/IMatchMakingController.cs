using System.Threading.Tasks;

public interface IMatchMakingController : IController
{
    Task StartAsHost();
    Task StartAsClient(string sessionName);
    Task CancelMatchmaking();
    Task AcceptMatch();
    Task RejectMatch();
}
