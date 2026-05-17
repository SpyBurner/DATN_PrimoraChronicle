using UnityObservables;

public interface IBattleSetupModel : IModel
{
    Observable<bool> FillRoomWithAI { get; }
    Observable<int> PlayerCnt { get; }

    void SetFillRoomWithAI(bool fillRoomWithAI);
    void SetPlayerCnt(int playerCnt);
}
