using UnityObservables;

internal class BattleModel : IBattleModel
{
    private Observable<string> _opponentName = new(string.Empty);
    private Observable<int> _opponentLevel = new(0);
    private Observable<int> _playerHP = new(100);
    private Observable<int> _opponentHP = new(100);
    private Observable<int> _playerMaxHP = new(100);
    private Observable<int> _opponentMaxHP = new(100);
    private Observable<bool> _isReady = new(false);

    public Observable<string> OpponentName { get => _opponentName; }
    public Observable<int> OpponentLevel { get => _opponentLevel; }
    public Observable<int> PlayerHP { get => _playerHP; }
    public Observable<int> OpponentHP { get => _opponentHP; }
    public Observable<int> PlayerMaxHP { get => _playerMaxHP; }
    public Observable<int> OpponentMaxHP { get => _opponentMaxHP; }
    public Observable<bool> IsReady { get => _isReady; }

    public void Initialize() { }

    public void Dispose()
    {
        _opponentName.Value = string.Empty;
        _opponentLevel.Value = 0;
        _playerHP.Value = 100;
        _opponentHP.Value = 100;
        _playerMaxHP.Value = 100;
        _opponentMaxHP.Value = 100;
        _isReady.Value = false;
    }

    internal void SetOpponentName(string name) => _opponentName.Value = name;
    internal void SetOpponentLevel(int level) => _opponentLevel.Value = level;
    internal void SetPlayerHP(int hp) => _playerHP.Value = hp;
    internal void SetOpponentHP(int hp) => _opponentHP.Value = hp;
    internal void SetPlayerMaxHP(int maxHP) => _playerMaxHP.Value = maxHP;
    internal void SetOpponentMaxHP(int maxHP) => _opponentMaxHP.Value = maxHP;
    internal void SetIsReady(bool isReady) => _isReady.Value = isReady;
}
