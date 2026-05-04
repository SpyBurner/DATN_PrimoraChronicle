using UnityObservables;

public interface ILobbyMainModel : IModel
{
    Observable<string> Username { get; }
    Observable<int> Level { get; }
    Observable<int> Gold { get; }
    Observable<string> AvatarUrl { get; }
}
