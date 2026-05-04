using UnityObservables;

public interface IBattleModel : IModel
{
    Observable<string> OpponentName { get; }
    Observable<int> OpponentLevel { get; }
    Observable<int> PlayerHP { get; }
    Observable<int> OpponentHP { get; }
    Observable<int> PlayerMaxHP { get; }
    Observable<int> OpponentMaxHP { get; }
    Observable<bool> IsReady { get; }
}
