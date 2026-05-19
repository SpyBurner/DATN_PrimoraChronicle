using Zenject;

public class BattleSetupController : IBattleSetupController
{
    [Inject] private readonly IBattleSetupModel _model;
    [Inject] private readonly IDebugLogger _debugLogger;

    public void Initialize() { }
    public void Dispose() { }

    public void SetFillRoomWithAI(bool fillRoomWithAI) => _model.SetFillRoomWithAI(fillRoomWithAI);
    public void SetPlayerCnt(int playerCnt) => _model.SetPlayerCnt(playerCnt);
}
