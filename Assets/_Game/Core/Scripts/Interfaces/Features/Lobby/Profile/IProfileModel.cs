using UnityObservables;

public interface IProfileModel : IModel
{
    Observable<string> Username { get; }
    Observable<int> Level { get; }
    Observable<int> Xp { get; }
    Observable<int> XpToNextLevel { get; }
    Observable<int> Gold { get; }
    Observable<string> AvatarUrl { get; }

    public void SetUsername(string username);
    public void SetLevel(int level);
    public void SetXp(int xp);
    public void SetXpToNextLevel(int xpToNextLevel);
    public void SetGold(int gold);
    public void SetAvatarUrl(string url);
}
