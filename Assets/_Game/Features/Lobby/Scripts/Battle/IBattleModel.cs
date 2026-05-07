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

    void SetOpponentName(string name);
    void SetOpponentLevel(int level);
    void SetPlayerHP(int hp);
    void SetOpponentHP(int hp);
    void SetPlayerMaxHP(int maxHP);
    void SetOpponentMaxHP(int maxHP);
    void SetIsReady(bool isReady);
}
