using System.Threading.Tasks;
using Fusion;
using UnityEngine.Events;

public interface INetworkManagerSubsystem : ISubsystem
{
    event UnityAction<NetworkRunner.States> RunnerStateChanged;
    event UnityAction<string> SessionNameChanged;
    event UnityAction<int> PlayerCountChanged;
    event UnityAction<string> ErrorMessageChanged;

    NetworkRunner.States RunnerState { get; }
    string SessionName { get; }
    string Region { get; }
    int PlayerCount { get; }
    int MaxPlayers { get; }
    string ErrorMessage { get; }
    NetworkRunner Runner { get; }

    Task<bool> StartSession(StartGameArgs args);
    Task ShutdownRunner();
}
