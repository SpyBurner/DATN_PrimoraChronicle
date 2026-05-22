using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Unity.Multiplayer.PlayMode;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class BattlePanel : UIPanel
{
    [Inject] private readonly IBattleSetupSubsystem _battleSetup;
    [Inject] private readonly IUIManagerSubsystem _uiManager;
    [Inject] private readonly INetworkManagerSubsystem _networkManager;

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
        
        _fillRoomWithAI.isOn = false;
        _playerCountDropdown.value = 0;
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
        // This is for testing only.
        var args = new StartGameArgs
        {
            GameMode    = GameMode.AutoHostOrClient,
            SessionName = "test-host-session",
            PlayerCount = _battleSetup.PlayerCnt,
            SessionProperties = new Dictionary<string, SessionProperty>
            {
                { "ai_count", 1 }
            }
        };

        await _networkManager.StartSession(args);
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
