using UnityObservables;

internal class LobbyMainModel : ILobbyMainModel
{
    private Observable<string> _username = new(string.Empty);
    private Observable<int> _level = new(0);
    private Observable<int> _gold = new(0);
    private Observable<string> _avatarUrl = new(string.Empty);

    public Observable<string> Username { get => _username; }
    public Observable<int> Level { get => _level; }
    public Observable<int> Gold { get => _gold; }
    public Observable<string> AvatarUrl { get => _avatarUrl; }

    public void Initialize() { }

    public void Dispose()
    {
        _username.Value = string.Empty;
        _level.Value = 0;
        _gold.Value = 0;
        _avatarUrl.Value = string.Empty;
    }

    public void SetUsername(string username) => _username.Value = username;
    public void SetLevel(int level) => _level.Value = level;
    public void SetGold(int gold) => _gold.Value = gold;
    public void SetAvatarUrl(string url) => _avatarUrl.Value = url;
}
