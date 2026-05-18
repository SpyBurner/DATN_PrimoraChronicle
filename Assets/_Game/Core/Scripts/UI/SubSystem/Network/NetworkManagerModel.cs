using Fusion;
using UnityObservables;

public class NetworkManagerModel : INetworkManagerModel
{
    private Observable<NetworkRunner.States> _runnerState = new(NetworkRunner.States.Shutdown);
    private Observable<string> _sessionName = new(string.Empty);
    private Observable<string> _region = new(string.Empty);
    private Observable<int> _playerCount = new(0);
    private Observable<int> _maxPlayers = new(0);
    private Observable<string> _errorMessage = new(string.Empty);
    private Observable<PlayerRef> _lastJoinedPlayer = new(PlayerRef.None);
    private Observable<PlayerRef> _lastLeftPlayer = new(PlayerRef.None);
    private Observable<bool> _isSceneLoading = new(false);

    public Observable<NetworkRunner.States> RunnerState { get => _runnerState; private set => _runnerState = value; }
    public Observable<string> SessionName { get => _sessionName; private set => _sessionName = value; }
    public Observable<string> Region { get => _region; private set => _region = value; }
    public Observable<int> PlayerCount { get => _playerCount; private set => _playerCount = value; }
    public Observable<int> MaxPlayers { get => _maxPlayers; private set => _maxPlayers = value; }
    public Observable<string> ErrorMessage { get => _errorMessage; private set => _errorMessage = value; }
    public Observable<PlayerRef> LastJoinedPlayer { get => _lastJoinedPlayer; private set => _lastJoinedPlayer = value; }
    public Observable<PlayerRef> LastLeftPlayer { get => _lastLeftPlayer; private set => _lastLeftPlayer = value; }
    public Observable<bool> IsSceneLoading { get => _isSceneLoading; private set => _isSceneLoading = value; }

    public void Initialize() { }

    public void Dispose()
    {
        _runnerState.Value = NetworkRunner.States.Shutdown;
        _sessionName.Value = string.Empty;
        _region.Value = string.Empty;
        _playerCount.Value = 0;
        _maxPlayers.Value = 0;
        _errorMessage.Value = string.Empty;
        _lastJoinedPlayer.Value = PlayerRef.None;
        _lastLeftPlayer.Value = PlayerRef.None;
    }

    public void SetRunnerState(NetworkRunner.States state) => _runnerState.Value = state;
    public void SetSessionName(string sessionName) => _sessionName.Value = sessionName;
    public void SetRegion(string region) => _region.Value = region;
    public void SetPlayerCount(int count) => _playerCount.Value = count;
    public void SetMaxPlayers(int maxPlayers) => _maxPlayers.Value = maxPlayers;
    public void SetErrorMessage(string message) => _errorMessage.Value = message;
    public void SetLastJoinedPlayer(PlayerRef player) => _lastJoinedPlayer.Value = player;
    public void SetLastLeftPlayer(PlayerRef player) => _lastLeftPlayer.Value = player;
    public void SetIsSceneLoading(bool isLoading) => _isSceneLoading.Value = isLoading;
}
