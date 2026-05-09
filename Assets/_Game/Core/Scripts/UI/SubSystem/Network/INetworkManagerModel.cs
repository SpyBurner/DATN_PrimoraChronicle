using Fusion;
using UnityObservables;

public interface INetworkManagerModel : IModel
{
    Observable<NetworkRunner.States> RunnerState { get; }
    Observable<string> SessionName { get; }
    Observable<string> Region { get; }
    Observable<int> PlayerCount { get; }
    Observable<int> MaxPlayers { get; }
    Observable<string> ErrorMessage { get; }

    void SetRunnerState(NetworkRunner.States state);
    void SetSessionName(string sessionName);
    void SetRegion(string region);
    void SetPlayerCount(int count);
    void SetMaxPlayers(int maxPlayers);
    void SetErrorMessage(string message);
}
