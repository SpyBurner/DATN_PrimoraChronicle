using Fusion;
using UnityObservables;

internal class ServerSessionModel : IServerSessionModel
{
    private readonly Observable<string> _activeSessionName = new(string.Empty);
    private readonly Observable<bool> _isRunning = new(false);
    private readonly Observable<PlayerRef> _lastJoinedPlayer = new(PlayerRef.None);
    private readonly Observable<PlayerRef> _lastLeftPlayer = new(PlayerRef.None);

    public Observable<string> ActiveSessionName => _activeSessionName;
    public Observable<bool> IsRunning => _isRunning;
    public Observable<PlayerRef> LastJoinedPlayer => _lastJoinedPlayer;
    public Observable<PlayerRef> LastLeftPlayer => _lastLeftPlayer;

    public void Initialize()
    {
        _activeSessionName.Value = string.Empty;
        _isRunning.Value = false;
        _lastJoinedPlayer.Value = PlayerRef.None;
        _lastLeftPlayer.Value = PlayerRef.None;
    }

    public void Dispose()
    {
        _activeSessionName.Value = string.Empty;
        _isRunning.Value = false;
        _lastJoinedPlayer.Value = PlayerRef.None;
        _lastLeftPlayer.Value = PlayerRef.None;
    }

    public void ApplyState(ServerSessionStateData data)
    {
        _activeSessionName.Value = data.ActiveSessionName;
        _isRunning.Value = data.IsRunning;
        _lastJoinedPlayer.Value = data.LastJoinedPlayer;
        _lastLeftPlayer.Value = data.LastLeftPlayer;
    }
}
