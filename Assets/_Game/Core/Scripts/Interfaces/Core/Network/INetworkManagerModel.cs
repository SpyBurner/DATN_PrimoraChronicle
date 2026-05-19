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
    Observable<PlayerRef> LastJoinedPlayer { get; }
    Observable<PlayerRef> LastLeftPlayer { get; }
    Observable<bool> IsSceneLoading { get; }

    void SetRunnerState(NetworkRunner.States state);
    void SetSessionName(string sessionName);
    void SetRegion(string region);
    void SetPlayerCount(int count);
    void SetMaxPlayers(int maxPlayers);
    void SetErrorMessage(string message);
    void SetLastJoinedPlayer(PlayerRef player);
    void SetLastLeftPlayer(PlayerRef player);
    void SetIsSceneLoading(bool isLoading);
}
