using UnityObservables;

public interface IProfileModel : IModel
{
    Observable<string> Username { get; }
    Observable<int> Level { get; }
    Observable<int> Xp { get; }
    Observable<int> XpToNextLevel { get; }
    Observable<int> Gold { get; }
    Observable<string> AvatarUrl { get; }
}
