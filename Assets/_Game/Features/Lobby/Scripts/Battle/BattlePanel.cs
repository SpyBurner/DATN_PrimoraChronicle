using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class BattlePanel : UIPanel
{
    [Inject] private readonly IBattleSetupSubsystem _battleSetup;
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

    private void OnStartMatchmaking()
    {
        _battleSetup.StartMatchmaking();
    }
}
