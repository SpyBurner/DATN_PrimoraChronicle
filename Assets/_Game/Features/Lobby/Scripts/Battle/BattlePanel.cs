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

        _battleSetup.SetPlayerCnt(_dropdownOptions[0]);
        _battleSetup.SetFillRoomWithAI(false);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _startMatchmakingButton?.onClick.RemoveListener(OnStartMatchmaking);
        _playerCountDropdown?.onValueChanged.RemoveListener(OnPlayerCountChanged);
        _fillRoomWithAI?.onValueChanged.RemoveListener(OnFillRoomWithAIChanged);
    }

    private async void OnStartMatchmaking()
    {
        await _uiManager.Show<MatchMakingPanel>();
    }

    private void OnFillRoomWithAIChanged(bool isFillRoom)
    {
        _battleSetup.SetFillRoomWithAI(isFillRoom);
    }

    private void OnPlayerCountChanged(int choiceIndex)
    {
        var playerCnt = _dropdownOptions[choiceIndex];
        _battleSetup.SetPlayerCnt(playerCnt);

        _fillRoomWithAI.gameObject.SetActive(playerCnt == 2);
    }
}
