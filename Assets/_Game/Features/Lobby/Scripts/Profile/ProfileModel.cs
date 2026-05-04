using UnityObservables;

internal class ProfileModel : IProfileModel
{
    private Observable<string> _username = new(string.Empty);
    private Observable<int> _level = new(0);
    private Observable<int> _xp = new(0);
    private Observable<int> _xpToNextLevel = new(100);
    private Observable<int> _gold = new(0);
    private Observable<string> _avatarUrl = new(string.Empty);

    public Observable<string> Username { get => _username; }
    public Observable<int> Level { get => _level; }
    public Observable<int> Xp { get => _xp; }
    public Observable<int> XpToNextLevel { get => _xpToNextLevel; }
    public Observable<int> Gold { get => _gold; }
    public Observable<string> AvatarUrl { get => _avatarUrl; }

    public void Initialize() { }

    public void Dispose()
    {
        _username.Value = string.Empty;
        _level.Value = 0;
        _xp.Value = 0;
        _xpToNextLevel.Value = 100;
        _gold.Value = 0;
        _avatarUrl.Value = string.Empty;
    }

    internal void SetUsername(string username) => _username.Value = username;
    internal void SetLevel(int level) => _level.Value = level;
    internal void SetXp(int xp) => _xp.Value = xp;
    internal void SetXpToNextLevel(int xpToNextLevel) => _xpToNextLevel.Value = xpToNextLevel;
    internal void SetGold(int gold) => _gold.Value = gold;
    internal void SetAvatarUrl(string url) => _avatarUrl.Value = url;
}
