using System.Threading.Tasks;
using Fusion;

public interface INetworkManagerController : IController
{
    NetworkRunner Runner { get; }
    Task<bool> StartSession(StartGameArgs args);
    Task ShutdownRunner();
}
