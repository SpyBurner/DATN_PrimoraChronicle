using System;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class BattlePanel : UIPanel
{
    [Inject] private readonly IBattleSetupSubsystem _battleSetup;
    [Inject] private readonly IUIManagerSubsystem _uiManager;

    [SerializeField] private Button _startMatchmakingButton;
    [SerializeField] private TMPro.TMP_Dropdown _playerCountDropdown;
    [SerializeField] private Toggle _fillRoomWithAI;

    protected override void OnEnable()
    {
        base.OnEnable();
        _startMatchmakingButton?.onClick.AddListener(OnStartMatchmaking);
        _playerCountDropdown?.onValueChanged.AddListener(OnPlayerCountChanged);
        _fillRoomWithAI?.onValueChanged.AddListener(OnFillRoomWithAIChanged);

        _battleSetup.SetPlayerCount(_playerCountDropdown.value);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _startMatchmakingButton?.onClick.RemoveListener(OnStartMatchmaking);
        _playerCountDropdown?.onValueChanged.RemoveListener(OnPlayerCountChanged);
        _fillRoomWithAI?.onValueChanged.RemoveListener(OnFillRoomWithAIChanged);
    }

    private void OnStartMatchmaking()
    {
        _battleSetup.StartMatchmaking();
    }

    private void OnFillRoomWithAIChanged(bool isFillRoom)
    {
        _battleSetup.SetBotCount(isFillRoom ? 1 : 0);
    }

    private void OnPlayerCountChanged(int playerCnt)
    {
        _battleSetup.SetPlayerCount(playerCnt);

        var botMode = playerCnt == 2 && _fillRoomWithAI.isOn;
        _fillRoomWithAI.interactable = playerCnt == 2;
        _battleSetup.SetBotCount(botMode ? 1 : 0);
    }
}
