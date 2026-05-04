using System.Threading.Tasks;

public interface IMatchMakingController : IController
{
    Task StartMatchmaking();
    Task CancelMatchmaking();
}
