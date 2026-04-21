using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class BattlePanel : UIPanel
{
    [Inject] private readonly IBattleSubsystem _battle;

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

    private void OnStartMatchmaking() => _battle.StartMatchmaking();
}
