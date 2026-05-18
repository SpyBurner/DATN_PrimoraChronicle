using UnityEngine.Events;

public interface IBattleSetupSubsystem : ISubsystem
{
    event UnityAction<bool> FillRoomWithAIChanged;
    event UnityAction<int> PlayerCntChanged;

    // Passthrough values for MatchMaking or UI to read
    bool FillRoomWithAI { get; }
    int  PlayerCnt      { get; }

    void SetFillRoomWithAI(bool fillRoomWithAI);
    void SetPlayerCnt(int playerCnt);
}
