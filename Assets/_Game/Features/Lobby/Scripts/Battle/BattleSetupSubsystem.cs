using UnityEngine.Events;
using Zenject;

public class BattleSetupSubsystem : IBattleSetupSubsystem
{
    [Inject] private readonly IBattleSetupController _controller;
    [Inject] private readonly IBattleSetupModel _model;

    public event UnityAction<bool> FillRoomWithAIChanged;
    public event UnityAction<int> PlayerCntChanged;

    public bool FillRoomWithAI => _model.FillRoomWithAI.Value;
    public int  PlayerCnt      => _model.PlayerCnt.Value;

    public void Initialize()
    {
        if (_model?.FillRoomWithAI != null)
            _model.FillRoomWithAI.OnChanged += HandleFillRoomWithAIChanged;

        if (_model?.PlayerCnt != null)
            _model.PlayerCnt.OnChanged += HandlePlayerCntChanged;
    }

    public void Dispose()
    {
        if (_model?.FillRoomWithAI != null)
            _model.FillRoomWithAI.OnChanged -= HandleFillRoomWithAIChanged;

        if (_model?.PlayerCnt != null)
            _model.PlayerCnt.OnChanged -= HandlePlayerCntChanged;
    }

    public void SetFillRoomWithAI(bool fillRoomWithAI) => _controller.SetFillRoomWithAI(fillRoomWithAI);
    public void SetPlayerCnt(int playerCnt) => _controller.SetPlayerCnt(playerCnt);
    
    private void HandleFillRoomWithAIChanged()
    {
        try { FillRoomWithAIChanged?.Invoke(_model.FillRoomWithAI.Value); } catch { }
    }

    private void HandlePlayerCntChanged()
    {
        try { PlayerCntChanged?.Invoke(_model.PlayerCnt.Value); } catch { }
    }
}
