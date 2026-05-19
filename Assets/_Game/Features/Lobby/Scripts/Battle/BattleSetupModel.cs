using UnityObservables;

internal class BattleSetupModel : IBattleSetupModel
{
    private Observable<bool> _fillRoomWithAI = new(false);
    private Observable<int> _playerCnt = new(2);

    public Observable<bool> FillRoomWithAI { get => _fillRoomWithAI; }
    public Observable<int> PlayerCnt { get => _playerCnt; }

    public void Initialize() { }

    public void Dispose()
    {
        _fillRoomWithAI.Value = false;
        _playerCnt.Value = 2;
    }

    public void SetFillRoomWithAI(bool fillRoomWithAI) => _fillRoomWithAI.Value = fillRoomWithAI;
    public void SetPlayerCnt(int playerCnt) => _playerCnt.Value = playerCnt;
}
