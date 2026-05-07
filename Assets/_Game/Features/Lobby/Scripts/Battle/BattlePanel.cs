using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class BattlePanel : UIPanel
{
    [Inject] private readonly IBattleSubsystem _battle;
    [Inject] private readonly IUIManagerSubsystem _uiManager;

    [SerializeField] private Button _startMatchmakingButton;

    protected override void OnEnable()
    {
        base.OnEnable();
        _startMatchmakingButton?.onClick.AddListener(OnStartMatchmaking);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _startMatchmakingButton?.onClick.RemoveListener(OnStartMatchmaking);
    }

    // D2: Navigation in View via UIManager — IBattleSubsystem has no StartMatchmaking
    private void OnStartMatchmaking() {
        // Pending changes
        //_uiManager.ShowScreen<MatchMakingPanel>()
    }
}

