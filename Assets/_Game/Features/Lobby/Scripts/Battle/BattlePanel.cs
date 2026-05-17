using System;
using System.Collections.Generic;
using System.Linq;
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

    [SerializeField] private List<int> _dropdownOptions = new() { 2, 3 };

    protected override void OnEnable()
    {
        base.OnEnable();
        _startMatchmakingButton?.onClick.AddListener(OnStartMatchmaking);
        _playerCountDropdown?.onValueChanged.AddListener(OnPlayerCountChanged);
        _fillRoomWithAI?.onValueChanged.AddListener(OnFillRoomWithAIChanged);

        _playerCountDropdown.ClearOptions();

        _playerCountDropdown.AddOptions(_dropdownOptions.Select(x => x.ToString()).ToList());

        _battleSetup.SetPlayerCount(_dropdownOptions[0]);
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

    private void OnPlayerCountChanged(int choiceIndex)
    {
        var playerCnt = _dropdownOptions[choiceIndex];
        _battleSetup.SetPlayerCount(playerCnt);

        var botMode = playerCnt == 2 && _fillRoomWithAI.isOn;
        _fillRoomWithAI.enabled = playerCnt == 2;

        var botCnt = botMode ? 1 : 0;
        _battleSetup.SetBotCount(botCnt);
        
    }
}
